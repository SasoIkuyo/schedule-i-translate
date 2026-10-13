using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ScheduleISpanish;

public static class AutoUpdater
{
    public static string FileName(string variant)=>variant switch {
        "Online"=>"ScheduleTranslate.dll", "Offline"=>"ScheduleISpanish.dll", _=>throw new ArgumentException("Unknown variant")};

    public static byte[] VerifyArchive(byte[] archive,string digest,string variant,Version version)
    {
        string actual="sha256:"+Convert.ToHexString(SHA256.HashData(archive)).ToLowerInvariant();
        if(!string.Equals(actual,digest,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Download checksum mismatch");
        using var zip=new ZipArchive(new MemoryStream(archive),ZipArchiveMode.Read);
        string name=FileName(variant);
        if(zip.Entries.Count!=1 || zip.Entries[0].FullName!="Mods/"+name || zip.Entries[0].Length>16*1024*1024)
            throw new InvalidDataException("Unexpected ZIP contents");
        using var input=zip.Entries[0].Open(); using var output=new MemoryStream();
        input.CopyTo(output); byte[] dll=output.ToArray();
        using var pe=new PEReader(new MemoryStream(dll));
        var metadata=pe.GetMetadataReader(); var assembly=metadata.GetAssemblyDefinition();
        if(metadata.GetString(assembly.Name)!=Path.GetFileNameWithoutExtension(name) ||
           assembly.Version!=new Version(version.Major,version.Minor,Math.Max(0,version.Build),Math.Max(0,version.Revision)))
            throw new InvalidDataException("Unexpected DLL identity or version");
        return dll;
    }

    public static async Task DownloadAndScheduleAsync(string gameRoot,string variant,Version version)
    {
        FileName(variant);
        using var client=new HttpClient {Timeout=TimeSpan.FromSeconds(60),MaxResponseContentBufferSize=8*1024*1024};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ScheduleITranslate-Updater/1.4.6");
        string json=await client.GetStringAsync("https://api.github.com/repos/SasoIkuyo/schedule-i-translate/releases?per_page=100").ConfigureAwait(false);
        using var document=JsonDocument.Parse(json);
        var release=document.RootElement.EnumerateArray().First(r=>!r.GetProperty("draft").GetBoolean() &&
            Version.TryParse(r.GetProperty("tag_name").GetString()?.TrimStart('v','V'),out var candidate) && candidate==version);
        var asset=release.GetProperty("assets").EnumerateArray().First(a=>a.GetProperty("name").GetString()==$"ScheduleTranslate-{variant}-{version}.zip");
        if(asset.GetProperty("size").GetInt64()>8*1024*1024) throw new InvalidDataException("Download is too large");
        string digest=asset.GetProperty("digest").GetString() ?? throw new InvalidDataException("Missing checksum");
        var url=new Uri(asset.GetProperty("browser_download_url").GetString()!);
        if(url.Scheme!="https" || url.Host!="github.com" || !url.AbsolutePath.StartsWith("/SasoIkuyo/schedule-i-translate/releases/download/",StringComparison.Ordinal))
            throw new InvalidDataException("Unexpected download URL");
        byte[] bytes=VerifyArchive(await client.GetByteArrayAsync(url).ConfigureAwait(false),digest,variant,version);
        using var process=Process.GetCurrentProcess();
        string helper=PrepareInstall(gameRoot,variant,bytes,process.Id,process.StartTime.ToUniversalTime().Ticks);
        StartHelper(helper);
    }

    public static string PrepareInstall(string gameRoot,string variant,byte[] dll,int processId,long startTicks)
    {
        string root=Path.GetFullPath(gameRoot);
        string target=Path.Combine(root,"Mods",FileName(variant));
        if(!File.Exists(target)) throw new FileNotFoundException("Installed mod DLL not found",target);
        string folder=Path.Combine(root,"UserData","ScheduleISpanish","Updates",variant);
        Directory.CreateDirectory(folder);
        string staged=Path.Combine(folder,"pending.dll"),backup=Path.Combine(folder,"previous.dll"),status=Path.Combine(folder,"status.txt");
        File.WriteAllBytes(staged,dll);
        string oldHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target)));
        string newHash=Convert.ToHexString(SHA256.HashData(dll));
        string Quote(string text)=>"'"+text.Replace("'","''")+"'";
        // All filesystem targets are exact, internally derived paths beneath gameRoot.
        // File.Replace installs atomically and keeps the previous DLL for recovery.
        string script="$ErrorActionPreference = 'Stop'\n"+
            "$target = "+Quote(target)+"\n$staged = "+Quote(staged)+"\n$backup = "+Quote(backup)+"\n$status = "+Quote(status)+"\n"+
            "try {\n  $deadline = [DateTime]::UtcNow.AddDays(1)\n  while ($true) {\n"+
            "    $game = Get-Process -Id "+processId+" -ErrorAction SilentlyContinue\n"+
            "    if (!$game -or $game.StartTime.ToUniversalTime().Ticks -ne "+startTicks+") { break }\n"+
            "    if ([DateTime]::UtcNow -gt $deadline) { throw 'Timed out waiting for game exit' }\n    Start-Sleep -Milliseconds 500\n  }\n"+
            "  if ((Get-FileHash -LiteralPath $staged -Algorithm SHA256).Hash -ne '"+newHash+"') { throw 'Staged checksum mismatch' }\n"+
            "  if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne '"+oldHash+"') { throw 'Installed DLL changed; update cancelled' }\n"+
            "  $installed = $false\n  for ($attempt = 0; $attempt -lt 30; $attempt++) {\n"+
            "    try { [IO.File]::Replace($staged,$target,$backup); $installed = $true; break } catch { Start-Sleep -Milliseconds 1000 }\n  }\n"+
            "  if (!$installed) { throw 'DLL still locked or installation denied' }\n"+
            "  [IO.File]::WriteAllText($status,'installed')\n"+
            "} catch { [IO.File]::WriteAllText($status,'failed: '+$_.Exception.Message); exit 1 }\n";
        string helper=Path.Combine(folder,"install.ps1");
        File.WriteAllText(helper,script,new UTF8Encoding(true));
        File.WriteAllText(status,"waiting-for-game-exit");
        return helper;
    }

    public static void StartHelper(string helper)
    {
        string powershell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        var start=new ProcessStartInfo(powershell) {UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
        foreach(string arg in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-File",helper}) start.ArgumentList.Add(arg);
        using var process=Process.Start(start) ?? throw new IOException("Unable to start update installer");
    }
}
