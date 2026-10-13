using System.Reflection;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppTMPro;
using UnityEngine.UI;
using System.Text.Json;
using System.IO.Compression;

#if ONLINE
[assembly: MelonInfo(typeof(ScheduleISpanish.SpanishMod),"Schedule I Translate Online","1.4.6","Saso")]
#else
[assembly: MelonInfo(typeof(ScheduleISpanish.SpanishMod),"Schedule I Spanish Offline","1.4.6","Saso")]
#endif
[assembly: MelonGame("TVGS","Schedule I")]

namespace ScheduleISpanish;

public sealed class SpanishMod : MelonMod
{
    private Task<Version?>? updateCheck;
    private string? availableVersion;
    private bool updateDismissed;
    private UpdateNotice? updateNotice;
    private UpdateSettings updateSettings=new();
    private string updateDirectory="";
    private DateTime settingsWrite,nextSettingsRead;
    private Task? installTask;
    private bool updateScheduled,automaticRequested;
#if ONLINE
    private const string UpdateVariant="Online";
    private const bool DefaultUpdateChecks=true;
#else
    private const string UpdateVariant="Offline";
    private const bool DefaultUpdateChecks=false;
#endif
    private static TranslationEngine? engine;
    [ThreadStatic] private static bool replacing;
    private static int errors;
    private static string targetLanguage="es";
    private static int phoneTextScale=1;
    private static TMP_Text? dialogueTarget;
    private static string dialogueSource="",dialogueTranslation="";
    private static StreamWriter? capture;
    private static readonly HashSet<string> missing = new(StringComparer.Ordinal);
#if ONLINE
    private static OnlineTranslator? online;
    private static readonly Dictionary<string,List<object>> waiting=new(StringComparer.Ordinal);
    private static DateTime nextRetry;
    private static int networkErrors;
#endif
    public override void OnInitializeMelon()
    {
        try
        {
#if ONLINE
            if(AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name=="ScheduleISpanish"))
            {
                LoggerInstance.Warning("Both translation variants are installed. Online disabled; remove ScheduleISpanish.dll to use ScheduleTranslate.dll.");
                return;
            }
#endif
            using var resource=typeof(SpanishMod).Assembly.GetManifestResourceStream("ScheduleISpanish.Dictionary.gz") ?? throw new InvalidDataException("Diccionario integrado ausente");
            using var decompressed=new GZipStream(resource,CompressionMode.Decompress);
            using var reader=new StreamReader(decompressed,System.Text.Encoding.UTF8);
            string parent=Path.Combine(MelonEnvironment.GameRootDirectory,"UserData","ScheduleISpanish");
            try { var display=DisplaySettings.Load(parent); phoneTextScale=display.PhoneTextScale; PhoneText.Enabled=display.UseSdfPhoneText; }
            catch(Exception ex) { LoggerInstance.Warning("display.json: "+ex.Message+" Using original phone text scale."); }
#if ONLINE
            try { targetLanguage=LanguageSettings.Load(parent).TargetLanguage; }
            catch(Exception ex) { LoggerInstance.Warning("language.json: "+ex.Message+" Using es."); }
#endif
            try
            {
#if ONLINE
                const bool defaultUpdates=true;
                const string variant="Online";
#else
                const bool defaultUpdates=false;
                const string variant="Offline";
#endif
                updateDirectory=parent;
                var updates=updateSettings=UpdateSettings.Load(parent,defaultUpdates);
                settingsWrite=File.GetLastWriteTimeUtc(Path.Combine(parent,"updates.json"));
                if(updates.PreviewNotification) availableVersion="PRUEBA / TEST";
                if(updates.CheckForUpdates)
                    updateCheck=Task.Run(()=>UpdateChecker.CheckAsync(typeof(SpanishMod).Assembly.GetName().Version!,variant));
            }
            catch(Exception ex) { LoggerInstance.Warning("updates.json: "+ex.Message); }
            engine=new TranslationEngine(); engine.Load(reader,identitiesOnly:targetLanguage!="es");
#if ONLINE
            if(targetLanguage!="en") online=new OnlineTranslator(engine,parent,targetLanguage:targetLanguage);
            LoggerInstance.Msg("Target language: "+targetLanguage+". Change UserData/ScheduleISpanish/language.json and restart to select another language.");
#endif
            if(File.Exists(Path.Combine(parent,"CaptureMissing.flag")))
                capture=new StreamWriter(Path.Combine(parent,"missing-runtime.jsonl"),append:true,System.Text.Encoding.UTF8,65536);
            var patcher=new HarmonyLib.Harmony("saso.schedule.spanish");
            try
            {
                var propertyType=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="Assembly-CSharp")?.GetType("Il2CppScheduleOne.Property.Property");
                var nameGetter=propertyType==null ? null : AccessTools.PropertyGetter(propertyType,"PropertyName");
                if(nameGetter!=null) patcher.Patch(nameGetter,postfix:new HarmonyMethod(typeof(SpanishMod).GetMethod(nameof(RegisterPropertyName),BindingFlags.Static|BindingFlags.NonPublic)));
            }
            catch(Exception ex) { LoggerInstance.Warning("Property name protection: "+ex.Message); }
            var prefix=new HarmonyMethod(typeof(SpanishMod).GetMethod(nameof(TextPrefix),BindingFlags.Static|BindingFlags.NonPublic));
            int hooks=0;
            // Only three text classes are inspected. No scene scans or per-frame polling.
            foreach(var type in new[]{typeof(TMP_Text),typeof(Text)})
            foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
            {
                var parameters=method.GetParameters();
                if((method.Name=="set_text" || method.Name=="SetText") && parameters.Length>0 && parameters[0].ParameterType==typeof(string))
                {
                    try { patcher.Patch(method,prefix:prefix,postfix:type==typeof(Text) && method.Name=="set_text" ? new HarmonyMethod(typeof(PhoneText).GetMethod(nameof(PhoneText.TextChanged),BindingFlags.Static|BindingFlags.NonPublic)) : null); hooks++; }
                    catch(Exception ex) { LoggerInstance.Warning("No se pudo interceptar "+type.Name+"."+method.Name+": "+ex.Message); }
                }
            }
            HookEnable(patcher,typeof(Text),nameof(EnableUGUI));
            HookEnable(patcher,typeof(TextMeshProUGUI),nameof(EnableTMP));
            HookEnable(patcher,typeof(TextMeshPro),nameof(EnableTMP));
            try { DialogueLayout.Install(patcher); }
            catch(Exception ex) { LoggerInstance.Warning("Dialogue choice layout: "+ex.Message); }
            if(PhoneText.Enabled)
            {
                try
                {
                    patcher.Patch(AccessTools.DeclaredMethod(typeof(Text),"OnPopulateMesh",new[]{typeof(VertexHelper)}),postfix:new HarmonyMethod(typeof(PhoneText).GetMethod(nameof(PhoneText.Populate),BindingFlags.Static|BindingFlags.NonPublic)));
                    patcher.Patch(AccessTools.DeclaredMethod(typeof(Text),"OnDisable"),postfix:new HarmonyMethod(typeof(PhoneText).GetMethod(nameof(PhoneText.Disable),BindingFlags.Static|BindingFlags.NonPublic)));
                }
                catch(Exception ex) { PhoneText.Enabled=false; LoggerInstance.Warning("Phone SDF font disabled: "+ex.Message); }
            }
            try
            {
                var dialogueType=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="Assembly-CSharp")?.GetType("Il2CppScheduleOne.UI.DialogueCanvas");
                var rollout=dialogueType==null ? null : AccessTools.DeclaredMethod(dialogueType,"RolloutDialogue");
                if(rollout!=null) patcher.Patch(rollout,prefix:new HarmonyMethod(typeof(SpanishMod).GetMethod(nameof(DialoguePrefix),BindingFlags.Static|BindingFlags.NonPublic)));
            }
            catch(Exception ex) { LoggerInstance.Warning("Dialogue rollout: "+ex.Message); }
            if(phoneTextScale>1)
            {
                try { patcher.Patch(AccessTools.PropertyGetter(typeof(Text),"pixelsPerUnit"),postfix:new HarmonyMethod(typeof(SpanishMod).GetMethod(nameof(PhonePixels),BindingFlags.Static|BindingFlags.NonPublic))); }
                catch(Exception ex) { LoggerInstance.Warning("Phone text clarity: "+ex.Message); }
            }
            LoggerInstance.Msg($"Diccionario integrado: {engine.EntryCount} textos, {engine.RuleCount} patrones, {engine.NumericCount} plantillas numericas. {hooks} setters.");
#if ONLINE
            LoggerInstance.Msg("Modo online: solo frases nuevas, cola limitada y cache persistente. Servicio: Google.");
#else
            LoggerInstance.Msg("Modo offline: traduccion sin conexion; comprobacion de actualizaciones opcional.");
#endif
        }
        catch(Exception ex) { LoggerInstance.Error("No se pudo iniciar la traduccion local: "+ex); engine=null; }
    }

    private static void HookEnable(HarmonyLib.Harmony patcher,Type type,string name)
    {
        try
        {
            var method=AccessTools.DeclaredMethod(type,"OnEnable");
            if(method!=null) patcher.Patch(method,postfix:new HarmonyMethod(typeof(SpanishMod).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)));
        }
        catch(Exception ex) { MelonLogger.Warning("No se pudo interceptar OnEnable de "+type.Name+": "+ex.Message); }
    }

    private static void PhonePixels(Text __instance,ref float __result)
    {
        try
        {
            if(phoneTextScale<=1 || __instance.font==null || !__instance.font.dynamic || __instance.canvas==null || __instance.canvas.renderMode!=UnityEngine.RenderMode.WorldSpace) return;
            // A higher rasterization density with reciprocal vertex scaling preserves layout.
            // Restrict this to the inspected phone hierarchy; no camera or scene resolution changes.
            var parent=__instance.transform;
            for(int i=0;i<16 && parent!=null;i++,parent=parent.parent)
                if(parent.name=="AppsCanvas") { __result*=phoneTextScale; return; }
        }
        catch(Exception ex) { Warn(ex); }
    }

    private static void RegisterPropertyName(string __result) { if(__result!=null) PlaceNames.Register(__result); }

    private static void TextPrefix(object __instance,ref string __0)
    {
        if(engine==null || replacing || string.IsNullOrEmpty(__0)) return;
        if(__instance is TMP_Text phoneMirror && (PhoneText.IsMirror(phoneMirror) || UpdateNotice.IsLabel(phoneMirror))) return;
        if(dialogueTarget!=null && __instance is TMP_Text tmp && tmp.GetInstanceID()==dialogueTarget.GetInstanceID() && __0!=dialogueSource && __0!=dialogueTranslation) return;
        try { if(__instance is TMP_Text label) FitSaveSlot(label); string original=__0; __0=Render(original,engine.Translate(__0)); if(__instance is TMP_Text choice && DialogueLayout.IsChoice(choice) && (__0!=original || engine.ContainsOriginal(original))) __0=TextFormatting.CapitalizeFirstVisible(__0); Capture(original,__0); Track(__instance,original,__0); }
        catch(Exception ex) { Warn(ex); }
    }

    private static void DialoguePrefix(object __instance,ref string __0)
    {
        if(engine==null || string.IsNullOrEmpty(__0)) return;
        try
        {
            dialogueTarget=AccessTools.Property(__instance.GetType(),"dialogueText")?.GetValue(__instance) as TMP_Text;
            dialogueSource=__0;
            __0=Render(dialogueSource,engine.Translate(dialogueSource));
            dialogueTranslation=__0;
            Capture(dialogueSource,__0);
            if(dialogueTarget!=null) Track(dialogueTarget,dialogueSource,__0);
        }
        catch(Exception ex) { Warn(ex); }
    }

    private static void EnableUGUI(Text __instance)
    {
        if(engine==null || replacing) return;
        try
        {
            PhoneText.Prepare(__instance);
            string original=__instance.text, translated=Render(original,engine.Translate(original));
            Capture(original,translated);
            Track(__instance,original,translated);
            if(original==translated) return;
            replacing=true; __instance.text=translated;
        }
        catch(Exception ex) { Warn(ex); }
        finally { replacing=false; }
    }

    private static void EnableTMP(TMP_Text __instance)
    {
        if(engine==null || replacing) return;
        if(PhoneText.IsMirror(__instance) || UpdateNotice.IsLabel(__instance)) return;
        try
        {
            FitSaveSlot(__instance);
            string original=__instance.text, translated=Render(original,engine.Translate(original));
            if(DialogueLayout.IsChoice(__instance) && (translated!=original || engine.ContainsOriginal(original))) translated=TextFormatting.CapitalizeFirstVisible(translated);
            Capture(original,translated);
            Track(__instance,original,translated);
            if(original==translated) return;
            replacing=true; __instance.text=translated;
        }
        catch(Exception ex) { Warn(ex); }
        finally { replacing=false; }
    }

    private static void FitSaveSlot(TMP_Text label)
    {
        var group=label.transform;
        if(group.name=="Text") group=group.parent;
        if(group==null || (group.name!="NetWorth" && group.name!="Created" && group.name!="LastPlayed") || group.parent==null || group.parent.name!="Info") return;
        var caption=group.GetComponent<TextMeshProUGUI>();
        var valueTransform=group.Find("Text");
        var value=valueTransform==null ? null : valueTransform.GetComponent<TextMeshProUGUI>();
        if(caption==null || value==null) return;
        // Reserve separate spaces inside each pair's existing width.
        float width=caption.rectTransform.rect.width;
        if(width<=0) return;
        float captionWidth=width*(58f/150f),valueStart=width*(64f/150f);
        caption.margin=new UnityEngine.Vector4(0,0,width-captionWidth,0);
        var rect=value.rectTransform;
        rect.anchorMin=rect.anchorMax=new UnityEngine.Vector2(0,0.5f);
        rect.pivot=new UnityEngine.Vector2(0,0.5f);
        rect.anchoredPosition=new UnityEngine.Vector2(valueStart,0);
        rect.sizeDelta=new UnityEngine.Vector2(width-valueStart,25);
        value.margin=UnityEngine.Vector4.zero;
        foreach(var text in new TMP_Text[]{caption,value})
        {
            text.enableWordWrapping=false;
            text.enableAutoSizing=true;
            text.fontSizeMin=8;
            text.fontSizeMax=12;
            text.overflowMode=TextOverflowModes.Ellipsis;
        }
    }

    private static string Render(string original,string translated) => targetLanguage!="es" && targetLanguage!="en" && original==translated ? translated : SupportMessage.Append(original,translated,spanish:targetLanguage=="es");

    private static void Warn(Exception ex)
    {
        if(errors++<3) MelonLogger.Warning("Etiqueta no traducida: "+ex.Message);
    }

    private static void Capture(string original,string translated)
    {
        if(capture==null || engine==null || original!=translated || TranslationFilter.IsNonLinguistic(original) || engine.ContainsOriginal(original) || missing.Count>=2048) return;
        if(original.Length>=3 && original.Any(char.IsLetter) && missing.Add(original))
            capture.WriteLine(JsonSerializer.Serialize(original));
    }

    private static void Track(object target,string original,string translated)
    {
#if ONLINE
        if(original!=translated || SupportMessage.IsSupportMessage(original) || online==null || !online.IsEligible(original)) return;
        if(target is TMP_Text tmp && !tmp.isActiveAndEnabled || target is Text text && !text.isActiveAndEnabled) return;
        if(!waiting.TryGetValue(original,out var targets))
        {
            if(waiting.Count>=128) return;
            waiting[original]=targets=new List<object>();
        }
        if(targets.Count<4 && !targets.Contains(target)) targets.Add(target);
        online.Request(original);
#endif
    }

    public override void OnLateInitializeMelon()
    {
        // Mod Manager 2.2.x resolves JSON paths by mod display name with spaces replaced.
        // No dependency on Mod Manager is needed for translation or updating.
        try
        {
            if(updateDirectory.Length==0 || !MelonBase.RegisteredMelons.Any(m=>m.GetType().Namespace=="ModManagerPhoneApp")) return;
            var category=MelonPreferences.GetCategory("ModManager&PhoneApp_JsonPaths") ?? MelonPreferences.CreateCategory("ModManager&PhoneApp_JsonPaths","JSON File Paths");
            string key=Info.Name.Replace(" ","_");
            var entry=category.GetEntry<string>(key);
            string path=Path.Combine(updateDirectory,"updates.json");
            if(entry==null) category.CreateEntry<string>(key,path,"Schedule I Translate updates.json");
            else if(string.IsNullOrWhiteSpace(entry.Value)) entry.Value=path;
            MelonPreferences.Save();
        }
        catch(Exception ex) { LoggerInstance.Warning("Mod Manager JSON settings: "+ex.Message); }
    }

    private void ReloadUpdateSettings()
    {
        if(updateDirectory.Length==0 || DateTime.UtcNow<nextSettingsRead) return;
        nextSettingsRead=DateTime.UtcNow.AddSeconds(2);
        string file=Path.Combine(updateDirectory,"updates.json");
        var written=File.GetLastWriteTimeUtc(file);
        if(written==settingsWrite) return;
        settingsWrite=written;
        try
        {
            var previous=updateSettings;
            updateSettings=UpdateSettings.Load(updateDirectory,DefaultUpdateChecks);
            updateNotice?.Dispose(); updateNotice=null; updateDismissed=false;
            if(updateSettings.PreviewNotification && availableVersion==null) availableVersion="PRUEBA / TEST";
            else if(!updateSettings.PreviewNotification && availableVersion=="PRUEBA / TEST") availableVersion=null;
            if(updateSettings.CheckForUpdates && !previous.CheckForUpdates && updateCheck==null)
                updateCheck=Task.Run(()=>UpdateChecker.CheckAsync(typeof(SpanishMod).Assembly.GetName().Version!,UpdateVariant));
        }
        catch(Exception ex) { LoggerInstance.Warning("updates.json: "+ex.Message); }
    }

    private void BeginUpdate()
    {
        if(installTask!=null || updateScheduled || !Version.TryParse(availableVersion,out var version)) return;
        if(!updateSettings.AllowInGameUpdates) { UnityEngine.Application.OpenURL(UpdateChecker.ReleasesUrl); return; }
        updateNotice?.Status(targetLanguage=="es" ? "Descargando y verificando..." : "Downloading and verifying...",targetLanguage=="es" ? "Descargando..." : "Downloading...",false);
        installTask=Task.Run(()=>AutoUpdater.DownloadAndScheduleAsync(MelonEnvironment.GameRootDirectory,UpdateVariant,version));
    }

    public override void OnUpdate()
    {
        ReloadUpdateSettings();
        if(installTask is { IsCompleted:true })
        {
            var completedInstall=installTask; installTask=null;
            updateScheduled=completedInstall.IsCompletedSuccessfully;
            if(completedInstall.IsFaulted) LoggerInstance.Warning("Update download: "+completedInstall.Exception?.GetBaseException().Message);
            updateNotice?.Status(updateScheduled ? (targetLanguage=="es" ? "Cierra el juego para instalar" : "Close the game to install") :
                (targetLanguage=="es" ? "Descarga fallida. Puedes reintentar." : "Download failed. Try again."),
                updateScheduled ? (targetLanguage=="es" ? "Lista para instalar" : "Ready to install") : (targetLanguage=="es" ? "Reintentar" : "Retry"),!updateScheduled);
        }
        if(updateCheck is { IsCompleted:true })
        {
            var completed=updateCheck; updateCheck=null;
            if(completed.IsCompletedSuccessfully && completed.Result is Version version)
            {
                availableVersion=version.ToString();
                LoggerInstance.Msg("Nueva version disponible: "+version+" - "+UpdateChecker.ReleasesUrl);
            }
            else if(completed.IsFaulted) LoggerInstance.Warning("No se pudo comprobar actualizaciones: "+completed.Exception?.GetBaseException().Message);
        }
        if(availableVersion!=null && updateSettings.ShowUpdateNotice && !updateDismissed && updateNotice==null && TMP_Settings.defaultFontAsset!=null)
        {
            try
            {
                updateNotice=UpdateNotice.Create(availableVersion,targetLanguage=="es",updateSettings,BeginUpdate,()=>updateDismissed=true);
                if(installTask!=null) updateNotice.Status(targetLanguage=="es" ? "Descargando y verificando..." : "Downloading and verifying...",targetLanguage=="es" ? "Descargando..." : "Downloading...",false);
                else if(updateScheduled) updateNotice.Status(targetLanguage=="es" ? "Cierra el juego para instalar" : "Close the game to install",targetLanguage=="es" ? "Lista para instalar" : "Ready to install",false);
                else if(!Version.TryParse(availableVersion,out _)) updateNotice.Status(targetLanguage=="es" ? "Vista previa del aviso" : "Notification preview",targetLanguage=="es" ? "Vista previa" : "Preview",false);
            }
            catch(Exception ex) { updateDismissed=true; LoggerInstance.Warning("Update notice: "+ex.Message); }
        }
        if(!automaticRequested && updateSettings.CheckForUpdates && updateSettings.AllowInGameUpdates && updateSettings.AutoDownloadUpdates && Version.TryParse(availableVersion,out _))
        { automaticRequested=true; BeginUpdate(); }
#if ONLINE
        if(online==null) return;
        if(DateTime.UtcNow>=nextRetry)
        {
            nextRetry=DateTime.UtcNow.AddMilliseconds(500);
            foreach(var entry in waiting.ToArray())
            {
                entry.Value.RemoveAll(target=>!StillWaiting(target,entry.Key));
                if(entry.Value.Count==0) { waiting.Remove(entry.Key); continue; }
                online.Request(entry.Key);
            }
        }
        for(int i=0;i<8 && online.TryTake(out var result);i++)
        {
            if(result==null || !waiting.TryGetValue(result.Source,out var targets)) continue;
            if(result.Translation==null)
            {
                if(networkErrors++<3) LoggerInstance.Warning("Online translation failed; retry after cooldown. "+result.Error);
                continue;
            }
            waiting.Remove(result.Source);
            foreach(var target in targets)
            {
                try
                {
                    replacing=true;
                    if(target is TMP_Text tmp && tmp.text==result.Source) tmp.text=Render(result.Source,result.Translation);
                    else if(target is Text text && text.text==result.Source) text.text=Render(result.Source,result.Translation);
                }
                catch(Exception ex) { Warn(ex); }
                finally { replacing=false; }
            }
        }
#endif
    }

#if ONLINE
    private static bool StillWaiting(object target,string source)
    {
        try
        {
            if(target is TMP_Text tmp) return tmp.isActiveAndEnabled && (tmp.text==source || (dialogueTarget!=null && tmp.GetInstanceID()==dialogueTarget.GetInstanceID() && dialogueSource==source));
            return target is Text text && text.isActiveAndEnabled && text.text==source;
        }
        catch { return false; }
    }
#endif

    public override void OnDeinitializeMelon()
    {
        updateNotice?.Dispose(); updateNotice=null;
        capture?.Dispose(); capture=null;
        dialogueTarget=null;
        PhoneText.Clear();
#if ONLINE
        online?.Dispose(); online=null; waiting.Clear();
#endif
    }
}
