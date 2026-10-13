using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScheduleISpanish;

public sealed class OnlineTranslator : IDisposable
{
    public record Result(string Source,string? Translation,string? Error=null);
    private record CacheEntry(string Source,string Translation);
    private readonly TranslationEngine engine;
    private readonly string cachePath;
    private readonly string targetLanguage;
    private readonly HttpClient client;
    private readonly ConcurrentQueue<string> jobs=new();
    private readonly ConcurrentDictionary<string,byte> pending=new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string,DateTime> cooldown=new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<Result> completed=new();
    private readonly SemaphoreSlim signal=new(0);
    private readonly CancellationTokenSource stopping=new();
    private readonly Task worker;
    private int learnedCount;
    private const int QueueLimit=64,CacheLimit=10000;
    private static readonly Regex Tokens=new(@"<[^>]*>|\{[^{}]*\}|\$[0-9]+|\\[nrt]|%[sdif]",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
    private static readonly Regex ProtectedNames=new(Tokens+@"|\b(?:Granddaddy Purple|Green Crack|Sour Diesel|OG Kush|Albert Hoover|Hyland Manor|Westville)\b",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
    private static readonly Regex SpanishTerms=new(ProtectedNames+@"|\b(?:weed seeds|weed|methamphetamine|meth|cocaine)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
    private static readonly Dictionary<string,string> Glossary=new(StringComparer.OrdinalIgnoreCase) { ["weed seeds"]="semillas de marihuana",["weed"]="marihuana",["methamphetamine"]="metanfetamina",["meth"]="metanfetamina",["cocaine"]="cocaína" };
    private readonly Func<string,CancellationToken,Task<string>>? transport;

    public OnlineTranslator(TranslationEngine engine,string directory,Func<string,CancellationToken,Task<string>>? transport=null,string targetLanguage="es")
    {
        this.engine=engine; this.transport=transport;
        this.targetLanguage=LanguageSettings.Validate(targetLanguage);
        Directory.CreateDirectory(directory);
        cachePath=Path.Combine(directory,$"cache.{this.targetLanguage}.jsonl");
        client=new HttpClient { Timeout=TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ScheduleTranslate/1.3");
        if(File.Exists(cachePath))
        {
            try
            {
                foreach(string line in File.ReadLines(cachePath))
                {
                    if(learnedCount>=CacheLimit) break;
                    try
                    {
                        var entry=JsonSerializer.Deserialize<CacheEntry>(line);
                        if(entry?.Source!=null && entry.Translation!=null && !TranslationFilter.IsNonLinguistic(entry.Source) && !engine.ContainsOriginal(entry.Source) && Valid(entry.Source,entry.Translation) && engine.AddLearned(entry.Source,TextFormatting.CapitalizeFirstVisible(entry.Translation))) learnedCount++;
                    }
                    catch(Exception ex) when(ex is JsonException or RegexMatchTimeoutException) { }
                }
            }
            catch(IOException) { }
        }
        worker=Task.Run(Work);
    }

    public bool IsEligible(string source) => !stopping.IsCancellationRequested && source.Length>=3 && source.Length<=2000 && source.Any(char.IsLetter) && !PlaceNames.IsName(source) && !TranslationFilter.IsNonLinguistic(source) && !engine.ContainsOriginal(source) && learnedCount<CacheLimit;

    public bool Request(string source)
    {
        if(!IsEligible(source)) return false;
        if(pending.ContainsKey(source)) return true;
        if(cooldown.TryGetValue(source,out var until) && until>DateTime.UtcNow) return false;
        if(pending.Count>=QueueLimit || !pending.TryAdd(source,0)) return false;
        jobs.Enqueue(source); signal.Release(); return true;
    }

    public bool TryTake(out Result? result) => completed.TryDequeue(out result);

    private async Task Work()
    {
        var token=stopping.Token;
        try
        {
            while(!token.IsCancellationRequested)
            {
                await signal.WaitAsync(token);
                if(!jobs.TryDequeue(out var source)) continue;
                try
                {
                    string value=TextFormatting.CapitalizeFirstVisible(await Translate(source,token));
                    // Persist first: failed disk writes do not report a saved translation.
                    await File.AppendAllTextAsync(cachePath,JsonSerializer.Serialize(new CacheEntry(source,value))+"\n",System.Text.Encoding.UTF8,token);
                    if(engine.AddLearned(source,value)) Interlocked.Increment(ref learnedCount);
                    completed.Enqueue(new Result(source,value));
                }
                catch(OperationCanceledException) when(token.IsCancellationRequested) { break; }
                catch(Exception ex)
                {
                    if(cooldown.Count>=128) cooldown.Clear();
                    cooldown[source]=DateTime.UtcNow.AddMinutes(2);
                    completed.Enqueue(new Result(source,null,ex.GetType().Name+": "+ex.Message));
                }
                finally { pending.TryRemove(source,out _); }
                await Task.Delay(1200,token);
            }
        }
        catch(OperationCanceledException) { }
    }

    private async Task<string> Translate(string source,CancellationToken token)
    {
        var parts=new List<string>();
        string protectedText=PlaceNames.Protect(source,value=> { parts.Add(value); return $"ZXQ{parts.Count-1:0000}QXZ"; });
        protectedText=RankLabels.ProtectNumerals(protectedText,value=> { parts.Add(value); return $"ZXQ{parts.Count-1:0000}QXZ"; });
        protectedText=(targetLanguage=="es" ? SpanishTerms : ProtectedNames).Replace(protectedText,m=> {
            string value=m.Value;
            if(targetLanguage=="es" && Glossary.TryGetValue(value,out var translated)) value=char.IsUpper(value[0]) ? char.ToUpperInvariant(translated[0])+translated[1..] : translated;
            parts.Add(value); return $"ZXQ{parts.Count-1:0000}QXZ";
        });
        string response;
        if(transport!=null) response=await transport(protectedText,token);
        else
        {
            string url="https://translate.googleapis.com/translate_a/single?client=gtx&dt=t&sl=en&tl="+Uri.EscapeDataString(targetLanguage)+"&q="+Uri.EscapeDataString(protectedText);
            string json=await client.GetStringAsync(url,token);
            using var document=JsonDocument.Parse(json);
            var builder=new System.Text.StringBuilder();
            foreach(var part in document.RootElement[0].EnumerateArray())
                if(part.GetArrayLength()>0 && part[0].ValueKind==JsonValueKind.String) builder.Append(part[0].GetString());
            response=builder.ToString();
        }
        for(int i=0;i<parts.Count;i++)
        {
            string marker=$"ZXQ{i:0000}QXZ";
            if(response.Split(marker).Length!=2) throw new FormatException("Formatting token changed");
            response=response.Replace(marker,parts[i],StringComparison.Ordinal);
        }
        if(!Valid(source,response)) throw new FormatException("Invalid translated formatting");
        return response;
    }

    private static bool Valid(string source,string value)
    {
        return source.Length<=2000 && value.Length>0 && value.Length<=8000 &&
            RankLabels.NumeralsPreserved(source,value) && PlaceNames.Preserved(source,value) &&
            Tokens.Matches(source).Cast<Match>().Select(m=>m.Value).OrderBy(s=>s).SequenceEqual(Tokens.Matches(value).Cast<Match>().Select(m=>m.Value).OrderBy(s=>s));
    }

    public void Dispose()
    {
        stopping.Cancel(); client.Dispose();
        // Do not block Unity's shutdown waiting on a network request.
        _=worker.ContinueWith(_=> { signal.Dispose(); stopping.Dispose(); },TaskScheduler.Default);
    }
}
