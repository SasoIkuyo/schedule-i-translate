using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ScheduleISpanish;

public sealed record UpdateSettings(bool CheckForUpdates=false,bool PreviewNotification=false,
    bool AllowInGameUpdates=true,bool AutoDownloadUpdates=false,bool ShowUpdateNotice=true,
    float NoticeOpacity=0.58f,float NoticeScale=1)
{
    public static UpdateSettings Load(string directory,bool enabledByDefault)
    {
        Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"updates.json");
        if(!File.Exists(path)) File.WriteAllText(path,JsonSerializer.Serialize(new UpdateSettings(enabledByDefault),new JsonSerializerOptions {WriteIndented=true}));
        var settings=JsonSerializer.Deserialize<UpdateSettings>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid updates.json");
        if(!float.IsFinite(settings.NoticeOpacity) || settings.NoticeOpacity is <0.15f or >0.9f ||
           !float.IsFinite(settings.NoticeScale) || settings.NoticeScale is <0.6f or >1.5f)
            throw new InvalidDataException("NoticeOpacity must be 0.15-0.9; NoticeScale must be 0.6-1.5");
        var existing=JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidDataException("Invalid updates.json");
        var defaults=JsonSerializer.SerializeToNode(settings)!.AsObject();
        bool changed=false;
        foreach(var field in defaults)
            if(!existing.ContainsKey(field.Key)) { existing[field.Key]=JsonNode.Parse(field.Value!.ToJsonString()); changed=true; }
        if(changed) File.WriteAllText(path,existing.ToJsonString(new JsonSerializerOptions {WriteIndented=true}));
        return settings;
    }
}

public static class UpdateChecker
{
    public const string ReleasesUrl="https://github.com/SasoIkuyo/schedule-i-translate/releases";
    public static Version? FindNewer(string json,Version current,string variant)
    {
        using var document=JsonDocument.Parse(json);
        Version? newest=null;
        foreach(var release in document.RootElement.EnumerateArray())
        {
            if(release.GetProperty("draft").GetBoolean()) continue;
            string tag=release.GetProperty("tag_name").GetString() ?? "";
            if(!Version.TryParse(tag.TrimStart('v','V'),out var version) || version<=current || (newest!=null && version<=newest)) continue;
            // Published previews are included: this project currently distributes prereleases.
            if(!release.GetProperty("assets").EnumerateArray().Any(a=>
                (a.GetProperty("name").GetString() ?? "").Equals($"ScheduleTranslate-{variant}-{version}.zip",StringComparison.OrdinalIgnoreCase))) continue;
            newest=version;
        }
        return newest;
    }

    public static async Task<Version?> CheckAsync(Version current,string variant)
    {
        using var client=new HttpClient {Timeout=TimeSpan.FromSeconds(8),MaxResponseContentBufferSize=2*1024*1024};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ScheduleITranslate/"+current);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        string json=await client.GetStringAsync("https://api.github.com/repos/SasoIkuyo/schedule-i-translate/releases?per_page=100").ConfigureAwait(false);
        return FindNewer(json,current,variant);
    }
}
