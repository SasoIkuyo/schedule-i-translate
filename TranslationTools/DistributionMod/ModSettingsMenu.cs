using MelonLoader;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ScheduleISpanish;

// Native MelonPreferences controls work in Mod Manager's main menu and phone app.
// The existing JSON files remain authoritative and work without Mod Manager.
internal sealed class ModSettingsMenu
{
    private readonly string directory;
    private readonly bool online;
    private readonly Action<string> log;
    private readonly MelonPreferences_Entry<string>? language;
    private readonly MelonPreferences_Entry<int> phoneScale;
    private readonly MelonPreferences_Entry<bool> phoneFont,check,preview,install,automatic,visible;
    private readonly MelonPreferences_Entry<float> opacity,scale;
    private Snapshot baseline;
    private DateTime nextRead;
    private string stamps="";
    private sealed record Snapshot(LanguageSettings Language,DisplaySettings Display,UpdateSettings Updates);

    internal ModSettingsMenu(string name,string directory,bool online,Action<string> log)
    {
        this.directory=directory; this.online=online; this.log=log;
        baseline=ReadFiles();
        // Dots are TOML path separators, so use underscores in category identifiers.
        MelonPreferences_Category Category(string suffix,string title)=>MelonPreferences.GetCategory(name+"_"+suffix) ?? MelonPreferences.CreateCategory(name+"_"+suffix,title);
        if(online)
        {
            var general=Category("Language","Idioma / Language");
            language=general.CreateEntry("TargetLanguage",baseline.Language.TargetLanguage,"Idioma de traducción (reiniciar)","Código: es, en, fr, de, pt, zh-CN... Requiere reiniciar para cambiar diccionario y caché.");
        }
        var phone=Category("Phone","Texto del teléfono / Phone text");
        phoneFont=phone.CreateEntry("UseSdfPhoneText",baseline.Display.UseSdfPhoneText,"Fuente nítida del teléfono (reiniciar)","Mejora la legibilidad. Los controles de cantidad y edición conservan su fuente original.");
        phoneScale=phone.CreateEntry("PhoneTextScale",baseline.Display.PhoneTextScale,"Nitidez del texto: 1 o 2 (reiniciar)","1 = original; 2 = más nítido. Mantiene el tamaño y la distribución.");
        var updates=Category("Updates","Actualizaciones y aviso / Updates and notice");
        check=updates.CreateEntry("CheckForUpdates",baseline.Updates.CheckForUpdates,"Buscar actualizaciones","Consulta GitHub al iniciar. Al activarlo durante la sesión, realiza una comprobación.");
        install=updates.CreateEntry("AllowInGameUpdates",baseline.Updates.AllowInGameUpdates,"Actualizar desde el juego","Descarga verificada e instalación al cerrar el juego. Si se desactiva, abre la página de descarga.");
        automatic=updates.CreateEntry("AutoDownloadUpdates",baseline.Updates.AutoDownloadUpdates,"Descargar automáticamente","Requiere buscar actualizaciones y permitir actualizar desde el juego. Instala al cerrar, con respaldo.");
        visible=updates.CreateEntry("ShowUpdateNotice",baseline.Updates.ShowUpdateNotice,"Mostrar aviso de actualización","Ocultar el aviso no cancela una actualización ya descargada.");
        opacity=updates.CreateEntry("NoticeOpacity",baseline.Updates.NoticeOpacity,"Opacidad del fondo: 0.15 a 0.9","Se aplica durante la sesión. Un valor menor hace el fondo más transparente.");
        scale=updates.CreateEntry("NoticeScale",baseline.Updates.NoticeScale,"Tamaño del aviso: 0.6 a 1.5","1 = tamaño normal. Se aplica durante la sesión.");
        preview=updates.CreateEntry("PreviewNotification",baseline.Updates.PreviewNotification,"Vista previa del aviso","Muestra el diseño sin necesitar una versión nueva. No descarga versiones ficticias.");
        if(language!=null) language.DefaultValue="es";
        phoneScale.DefaultValue=2; phoneFont.DefaultValue=true;
        check.DefaultValue=online; preview.DefaultValue=false; install.DefaultValue=true;
        automatic.DefaultValue=false; visible.DefaultValue=true; opacity.DefaultValue=0.58f; scale.DefaultValue=1;
        SetEntries(baseline);
        stamps=FileStamps();
        MelonPreferences.Save();
        log("Controles de configuración registrados: "+(online ? "idioma, " : "")+"teléfono, actualizaciones y aviso.");
    }

    private Snapshot ReadFiles()=>new(online ? LanguageSettings.Load(directory) : new LanguageSettings(),DisplaySettings.Load(directory),UpdateSettings.Load(directory,online));
    private string FileStamps()=>string.Join("|",new[]{"language.json","display.json","updates.json"}.Select(n=>File.GetLastWriteTimeUtc(Path.Combine(directory,n)).Ticks));
    private Snapshot ReadEntries()=>new(new LanguageSettings(language?.Value ?? "es"),new DisplaySettings(phoneScale.Value,phoneFont.Value),
        new UpdateSettings(check.Value,preview.Value,install.Value,automatic.Value,visible.Value,opacity.Value,scale.Value));

    private void SetEntries(Snapshot settings)
    {
        if(language!=null) language.Value=settings.Language.TargetLanguage;
        phoneScale.Value=settings.Display.PhoneTextScale; phoneFont.Value=settings.Display.UseSdfPhoneText;
        check.Value=settings.Updates.CheckForUpdates; preview.Value=settings.Updates.PreviewNotification;
        install.Value=settings.Updates.AllowInGameUpdates; automatic.Value=settings.Updates.AutoDownloadUpdates;
        visible.Value=settings.Updates.ShowUpdateNotice; opacity.Value=settings.Updates.NoticeOpacity; scale.Value=settings.Updates.NoticeScale;
    }

    private void SaveFile<T>(string name,T settings)
    {
        string path=Path.Combine(directory,name),temporary=path+".tmp";
        var content=File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path))!.AsObject() : new JsonObject();
        foreach(var field in JsonSerializer.SerializeToNode(settings)!.AsObject())
            content[field.Key]=JsonNode.Parse(field.Value!.ToJsonString());
        File.WriteAllText(temporary,content.ToJsonString(new JsonSerializerOptions {WriteIndented=true}));
        File.Move(temporary,path,true);
    }

    internal void Poll()
    {
        if(DateTime.UtcNow<nextRead) return;
        nextRead=DateTime.UtcNow.AddSeconds(2);
        try
        {
            var candidate=ReadEntries();
            if(candidate!=baseline)
            {
                candidate=candidate with {Language=new LanguageSettings(LanguageSettings.Validate(candidate.Language.TargetLanguage))};
                if(candidate.Display.PhoneTextScale is <1 or >2) throw new ArgumentException("PhoneTextScale: 1 o 2");
                if(!float.IsFinite(candidate.Updates.NoticeOpacity) || candidate.Updates.NoticeOpacity is <0.15f or >0.9f ||
                   !float.IsFinite(candidate.Updates.NoticeScale) || candidate.Updates.NoticeScale is <0.6f or >1.5f)
                    throw new ArgumentException("NoticeOpacity: 0.15-0.9; NoticeScale: 0.6-1.5");
                if(online && candidate.Language!=baseline.Language) SaveFile("language.json",candidate.Language);
                if(candidate.Display!=baseline.Display) SaveFile("display.json",candidate.Display);
                if(candidate.Updates!=baseline.Updates) SaveFile("updates.json",candidate.Updates);
                if(candidate.Language!=baseline.Language || candidate.Display!=baseline.Display)
                    log("Ajustes guardados. Reinicia el juego para aplicar idioma y texto del teléfono.");
                baseline=candidate; SetEntries(candidate); stamps=FileStamps();
                MelonPreferences.Save();
            }
            else
            {
                string current=FileStamps();
                if(current==stamps) return;
                stamps=current;
                var settings=ReadFiles(); SetEntries(settings); baseline=settings;
                MelonPreferences.Save();
            }
        }
        catch(Exception ex) { SetEntries(baseline); log("Configuración no aplicada: "+ex.Message); }
    }
}
