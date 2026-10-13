using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace ScheduleISpanish;

public sealed class TranslationEngine
{
    private readonly Dictionary<string,string> exact = new(StringComparer.Ordinal);
    private readonly Dictionary<string,string> normalized = new(StringComparer.Ordinal);
    private readonly Dictionary<string,NumericEntry> numbers = new(StringComparer.Ordinal);
    private readonly List<(Regex pattern,string replacement)> rules = new();
    private readonly List<DialogueTemplate> dialogueTemplates = new();
    private readonly Dictionary<string,string> cache = new(StringComparer.Ordinal);
    private readonly Queue<string> cacheOrder = new();
    private readonly ConcurrentDictionary<string,string> learned = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string,byte> learnedResults = new(StringComparer.Ordinal);
    private readonly HashSet<string> baseResults = new(StringComparer.Ordinal);
    private readonly Dictionary<string,int> derivedResults = new(StringComparer.Ordinal);
    private const int CacheLimit = 1024;
    private bool spanishGrammar;
    private static readonly Regex NumericTokens = new(@"<[^>]*>|\{[^{}]*\}|[0-9]+(?:[.,][0-9]+)?",RegexOptions.CultureInvariant);
    private static readonly Regex WrappedLabel = new(@"^(?<open>(?:<[^>]+>)+)(?<body>[^<>]+)(?<close>(?:</[^>]+>)+)$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(10));
    private static readonly Regex PricedLabel=new(@"^(?<label>[^<>\r\n]+?)(?<suffix>(?:\s*<[^>]+>)*\s*\(\$[0-9][0-9,.]*\)(?:</[^>]+>)*)$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(10));
    private sealed record NumericEntry(string[] Parts);
    private sealed record DialogueTemplate(string Prefix,Regex Pattern,string Translation,string[] Tokens);
    private static readonly Regex DialogueTokens=new(@"<(?:NAME|REGION|PRODUCT|LOCATION|PRICE|AMOUNT|TIME|WINDOW_START|WINDOW_END|DEBT|PAYMENT|PROPERTY|BUSINESS|VEHICLE|QUALITY|CUT|DAILY_WAGE|SIGNING_FEE|SIGN_FEE|DUE_DAYS|DIG_PRICE|NPC_DESCRIPTION)>",RegexOptions.CultureInvariant);
    public int EntryCount => exact.Count;
    public int RuleCount => rules.Count;
    public int NumericCount => numbers.Count;
    public int CachedCount => cache.Count;
    public bool ContainsOriginal(string source) => exact.ContainsKey(source) || learned.ContainsKey(source) || baseResults.Contains(source) || learnedResults.ContainsKey(source) || derivedResults.ContainsKey(source);
    public bool AddLearned(string source,string translation)
    {
        if(!learned.TryAdd(source,translation)) return false;
        learnedResults.TryAdd(translation,0); return true;
    }

    public void Load(string directory)
    {
        spanishGrammar=true;
        // Corrections are loaded last; identical keys replace earlier values.
        foreach (var file in Directory.GetFiles(directory,"*.txt").OrderBy(p => Path.GetFileName(p)=="LocalComplements.txt" ? 1 : 0))
        {
            using var reader=new StreamReader(file,Encoding.UTF8);
            ReadEntries(reader);
        }
        BuildNumbers();
    }

    public void Load(TextReader reader,bool identitiesOnly=false)
    {
        spanishGrammar=!identitiesOnly;
        ReadEntries(reader,identitiesOnly);
        BuildNumbers();
    }

    private void ReadEntries(TextReader reader,bool identitiesOnly=false)
    {
        string? raw;
        while((raw=reader.ReadLine())!=null)
        {
            var line=raw.TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            int separator=Separator(line);
            if (separator<0) continue;
            string key=Decode(line[..separator]),value=Decode(line[(separator+1)..]);
            if(identitiesOnly && (key!=value || key.StartsWith("r:") || key.StartsWith("sr:"))) continue;
            if (key.StartsWith("r:\"",StringComparison.Ordinal) && key.EndsWith('"'))
            {
                try { rules.Add((new Regex(key[3..^1],RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(10)),value)); }
                catch (ArgumentException) { /* Malformed patterns cannot break startup. */ }
                continue;
            }
            if (key.StartsWith("r:",StringComparison.Ordinal) || key.StartsWith("sr:",StringComparison.Ordinal)) continue;
            exact[key]=value;
            normalized[Normalize(key)]=value;
        }
    }

    private void BuildNumbers()
    {
        foreach (var (key,value) in exact)
        {
            baseResults.Add(value);
            var slots=DialogueTokens.Matches(key).Cast<Match>().ToArray();
            if(slots.Length>0 && slots[0].Index>=4 && slots.All(m=>value.Contains(m.Value,StringComparison.Ordinal)))
            {
                var pattern=new StringBuilder("^"); int offset=0;
                var tokens=new List<string>();
                foreach(var slot in slots)
                {
                    pattern.Append(Regex.Escape(key[offset..slot.Index]));
                    int index=tokens.IndexOf(slot.Value);
                    if(index<0) { index=tokens.Count; tokens.Add(slot.Value); pattern.Append($"(?<P{index}>[^\\r\\n]{{1,600}}?)"); }
                    else pattern.Append($"\\k<P{index}>");
                    offset=slot.Index+slot.Length;
                }
                pattern.Append(Regex.Escape(key[offset..])).Append('$');
                dialogueTemplates.Add(new(key[..slots[0].Index],new Regex(pattern.ToString(),RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(10)),value,tokens.ToArray()));
            }
            string template=NumberKey(key,out var source);
            NumberKey(value,out var target);
            if (source.Count==0 || !source.SequenceEqual(target)) continue;
            var parts=NumericParts(value);
            if (parts.Length==source.Count+1) numbers[template]=new NumericEntry(parts);
        }
    }

    public string Translate(string? source) => Translate(source,0);

    private string Translate(string? source,int depth) => string.IsNullOrEmpty(source) ? source ?? "" : PlaceNames.IsName(source) ? source : PlaceNames.Restore(source,TranslateCore(source,depth));

    private string TranslateCore(string? source,int depth)
    {
        if (string.IsNullOrEmpty(source) || source.Length>16000) return source ?? "";
        if(spanishGrammar && RankLabels.TrySpanish(source,out var rank)) return cache.TryGetValue(source,out var cachedRank) && cachedRank==rank ? cachedRank : Remember(source,rank);
        if (exact.TryGetValue(source,out var translated)) return translated;
        // Curated requirement wording takes precedence over older online cache entries.
        if(spanishGrammar && SpanishGrammar.TryRegionRequirement(source,out var requirement)) return cache.TryGetValue(source,out var cachedRequirement) && cachedRequirement==requirement ? cachedRequirement : Remember(source,requirement);
        if (learned.TryGetValue(source,out translated)) return translated;
        if (baseResults.Contains(source) || learnedResults.ContainsKey(source) || derivedResults.ContainsKey(source)) return source;
        if (cache.TryGetValue(source,out translated)) return translated;
        if (normalized.TryGetValue(Normalize(source),out translated)) return Remember(source,translated);
        var priced=PricedLabel.Match(source);
        if(priced.Success && exact.TryGetValue(priced.Groups["label"].Value,out var pricedName)) return Remember(source,pricedName+priced.Groups["suffix"].Value);
        if(spanishGrammar && SpanishGrammar.TryDuration(source,out var duration)) return Remember(source,duration);
        if(spanishGrammar && source.StartsWith("Use ",StringComparison.Ordinal) && exact.TryGetValue(source[4..],out var item)) return Remember(source,"Usar "+item);
        if (source[0]=='<' && depth<4)
        {
            try
            {
                var wrapper=WrappedLabel.Match(source);
                if(wrapper.Success)
                {
                    string body=Translate(wrapper.Groups["body"].Value,depth+1);
                    if(body!=wrapper.Groups["body"].Value) return Remember(source,wrapper.Groups["open"].Value+body+wrapper.Groups["close"].Value);
                }
            }
            catch(RegexMatchTimeoutException) { }
        }
        if (source.Any(char.IsDigit))
        {
            string template=NumberKey(source,out var digits);
            if (numbers.TryGetValue(template,out var entry) && digits.Count==entry.Parts.Length-1)
            {
                var sb=new StringBuilder(entry.Parts[0]);
                for (int i=0;i<digits.Count;i++) sb.Append(digits[i]).Append(entry.Parts[i+1]);
                return Remember(source,sb.ToString());
            }
        }
        foreach (var (pattern,replacement) in rules)
        {
            try
            {
                if (pattern.IsMatch(source)) return Remember(source,pattern.Replace(source,replacement));
            }
            catch (RegexMatchTimeoutException) { }
        }
        foreach(var template in dialogueTemplates)
        {
            if(!source.StartsWith(template.Prefix,StringComparison.Ordinal)) continue;
            try
            {
                var match=template.Pattern.Match(source);
                if(!match.Success) continue;
                string result=template.Translation;
                for(int i=0;i<template.Tokens.Length;i++)
                {
                    string captured=match.Groups[$"P{i}"].Value;
                    // Translate known item/region labels, preserving unknown names and amounts.
                    string slot=template.Tokens[i];
                    string value=slot is "<PRODUCT>" or "<VEHICLE>" or "<QUALITY>" ? exact.GetValueOrDefault(captured,captured) : captured;
                    if(spanishGrammar && slot=="<TIME>") value=SpanishGrammar.TranslateTimeSpan(value);
                    result=result.Replace(slot,value,StringComparison.Ordinal);
                }
                return Remember(source,result);
            }
            catch(RegexMatchTimeoutException) { }
        }
        if(depth<4)
        {
            if(source.Contains('\n'))
            {
                string lines=Regex.Replace(source,@"[^\r\n]+",m=>Translate(m.Value,depth+1));
                if(lines!=source) return Remember(source,lines);
            }
            var bullet=Regex.Match(source,@"^(?<prefix>\s*[•·]\s*)(?<body>.+)$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(10));
            if(bullet.Success)
            {
                string body=Translate(bullet.Groups["body"].Value,depth+1);
                if(body!=bullet.Groups["body"].Value) return Remember(source,bullet.Groups["prefix"].Value+body);
            }
        }
        return Remember(source,source);
    }

    private string Remember(string key,string value)
    {
        if (cache.Count>=CacheLimit)
        {
            string oldest=cacheOrder.Dequeue();
            if(cache.Remove(oldest,out var oldValue) && oldest!=oldValue && derivedResults.TryGetValue(oldValue,out var count))
            {
                if(count==1) derivedResults.Remove(oldValue); else derivedResults[oldValue]=count-1;
            }
        }
        if(key!=value) derivedResults[value]=derivedResults.GetValueOrDefault(value)+1;
        cache[key]=value; cacheOrder.Enqueue(key); return value;
    }

    private static string NumberKey(string value,out List<string> numbersFound)
    {
        var values=new List<string>();
        string key=NumericTokens.Replace(value,m => {
            if (!char.IsDigit(m.Value[0])) return m.Value;
            values.Add(m.Value); return "\uE000";
        });
        numbersFound=values;
        return key;
    }

    private static string[] NumericParts(string value)
    {
        var matches=NumericTokens.Matches(value).Cast<Match>().Where(m=>char.IsDigit(m.Value[0])).ToArray();
        var parts=new List<string>(); int start=0;
        foreach(var m in matches) { parts.Add(value[start..m.Index]); start=m.Index+m.Length; }
        parts.Add(value[start..]); return parts.ToArray();
    }

    private static string Normalize(string value)
    {
        var sb=new StringBuilder(value.Length); bool space=false;
        foreach(char c in value)
        {
            if (char.IsWhiteSpace(c)) { if(sb.Length>0) space=true; }
            else { if(space) sb.Append(' '); sb.Append(c); space=false; }
        }
        return sb.ToString();
    }

    private static int Separator(string line)
    {
        bool escaped=false;
        for (int i=0;i<line.Length;i++)
        {
            if(line[i]=='=' && !escaped) return i;
            if(line[i]=='\\') escaped=!escaped; else escaped=false;
        }
        return -1;
    }

    private static string Decode(string value)
    {
        var sb=new StringBuilder(value.Length);
        for(int i=0;i<value.Length;i++)
        {
            if(value[i]=='\\' && i+1<value.Length)
            {
                char next=value[i+1];
                if(next is 'n' or 'r' or 't' or '=' or '\\')
                { sb.Append(next switch {'n'=>'\n','r'=>'\r','t'=>'\t',_=>next}); i++; continue; }
            }
            sb.Append(value[i]);
        }
        return sb.ToString();
    }
}
