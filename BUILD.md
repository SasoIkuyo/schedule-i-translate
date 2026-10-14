# Compilar / Build

Esta rama publica el código de la versión 1.4.7 y sus pruebas para facilitar su revisión. Los commits agrupan los componentes existentes por función; no representan fechas originales de desarrollo. Los archivos del juego y las dependencias de MelonLoader no se distribuyen aquí.

## Requisitos

- SDK de .NET compatible con proyectos `net6.0`.
- Una instalación propia de Schedule I IL2CPP con MelonLoader y sus ensamblados generados en `MelonLoader/Il2CppAssemblies`. Inicia el juego con MelonLoader al menos una vez para generarlos.
- Referencias de MelonLoader en `MelonLoader/net6`.

Desde la raíz de este repositorio, ejecuta estos comandos de compilación en una terminal. Sustituye la ruta de ejemplo por la carpeta de tu juego:

```text
dotnet build TranslationTools/DistributionMod/DistributionMod.csproj -c Release -p:ModMode=Offline "-p:GameRoot=D:/SteamLibrary/steamapps/common/Schedule I"
dotnet build TranslationTools/DistributionMod/DistributionMod.csproj -c Release -p:ModMode=Online "-p:GameRoot=D:/SteamLibrary/steamapps/common/Schedule I"
dotnet run --project TranslationTools/DistributionTests/DistributionTests.csproj -c Release --no-launch-profile
```

Las DLL se generan en `TranslationTools/DistributionMod/bin/{Offline|Online}/Release/net6.0/`. Las pruebas usan esas dos compilaciones y simulan el transporte de red. La opción `-- --live` añade una consulta real a Google.

Estos comandos son para quienes quieran compilar o revisar el código. Para instalar el mod publicado basta copiar su DLL; no hay instaladores ni scripts que ejecutar.

## Archivos para revisar

| Archivo | Función |
| --- | --- |
| `SpanishGrammar.cs` | Tiempo restante con etiquetas de formato internas |
| `TranslationEngine.cs` | Diccionario, reglas, formatos numéricos y caché local limitada |
| `TranslationFilter.cs` | Evita consultas de contadores y valores sin contenido lingüístico |
| `TextFormatting.cs` | Mayúscula inicial conservando etiquetas y atajos |
| `OnlineTranslator.cs` | URL de Google, cola, protección de formato y caché persistente por idioma |
| `DisplaySettings.cs` | Densidad de rasterización del teléfono, reversible con `display.json` |
| `LanguageSettings.cs` | Lectura y validación de `language.json` |
| `SpanishMod.cs` | Interceptación de etiquetas Unity/TMP mediante Harmony y MelonLoader |
| `SupportMessage.cs` | Mensaje de apoyo y enlace del autor |
| `Dictionary.txt.gz` | Diccionario integrado, comprimido en gzip UTF-8 |
| `Dictionary.txt` | Copia legible del diccionario para revisión; no se compila |
| `DistributionTests/Program.cs` | Pruebas de formato, límites, caché y selección de idiomas |

El diccionario combina traducciones automáticas de textos extraídos del juego y correcciones locales. No se incluyen los inventarios completos del juego ni sus recursos originales. La copia legible corresponde al recurso gzip publicado; al modificar el diccionario para una compilación, también hay que regenerar el gzip.

La URL usada actualmente no lleva clave API, credenciales ni identificador de proyecto de Google Cloud. El código permite revisar qué texto se envía y qué se guarda localmente. Otros idiomas dependen de las respuestas de Google y de las fuentes disponibles en el juego.

## English

This branch publishes the source and tests for version 1.4.7. Commits group existing components by function, rather than representing their original development dates. Game files and MelonLoader dependencies are not bundled.

Install an SDK capable of building `net6.0` projects and generate MelonLoader's IL2CPP assemblies using your own game installation. Run the commands above from the repository root, replacing `GameRoot` with your game directory. Build both variants before running tests. Tests mock network requests; `-- --live` additionally contacts Google.

`Dictionary.txt` is the readable counterpart of the embedded `Dictionary.txt.gz`; only the gzip is embedded. The dictionary contains automated translations and local corrections. No full game inventories or original game assets are included.

Review `OnlineTranslator.cs` for the network endpoint, text sent, request limits and saved cache. The current endpoint uses no API key, owner credentials or Google Cloud project. Installing a release requires copying the DLL, with no scripts to run.

Online genera `ScheduleTranslate.dll`; Offline genera `ScheduleISpanish.dll`. `SpanishCorrections.txt` permite revisar las correcciones editoriales incorporadas en el diccionario; no se carga como archivo externo durante el juego. La reconstrucción usa el recurso gzip integrado.


Updater checks: `dotnet run --project TranslationTools/DistributionTests -c Release -- --updater-self-test` exercises the installer in an isolated folder, including process-exit waiting, SHA256, assembly version, variant, backup, Unicode paths and rejection of tampered payloads. Windows PowerShell is used only by the generated helper; it is not a bundled dependency. Source: `AutoUpdater.cs`, `UpdateChecker.cs`, `UpdateNotice.cs`, `PlaceNames.cs`.

`ModSettingsMenu.cs` exposes native MelonPreferences categories and synchronizes them with the existing JSON files. Language and phone changes require restarting. Update-notice changes apply during the session.
