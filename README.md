# Schedule I Translate

**Source branch:** [Build instructions](BUILD.md) | [Download compiled mod](https://github.com/SasoIkuyo/schedule-i-translate/releases)

Traducción de **Schedule I (IL2CPP)**: español sin conexión o traducción automática a otros idiomas mientras juegas. Funciona con **[MelonLoader](https://github.com/LavaGang/MelonLoader/releases)**.

**[Descargar](https://github.com/SasoIkuyo/schedule-i-translate/releases)** · [Código fuente](https://github.com/SasoIkuyo/schedule-i-translate/tree/source) · [Reportar un problema](https://github.com/SasoIkuyo/schedule-i-translate/issues) · [Apoyar el proyecto ☕](https://buymeacoffee.com/sasoikuyo)

[Español](#español) · [English](#english) · [Historial de cambios / Changelog](#historial-de-cambios--changelog)

> Las versiones actuales son preliminares. La traducción sigue en desarrollo y puede haber textos pendientes o ajustes visuales por corregir.

## Español

### Elige una versión

| Versión | Qué ofrece | Conexión | Archivo |
| --- | --- | --- | --- |
| **Offline** | 12.295 entradas integradas en español | No necesita internet | `ScheduleISpanish.dll` |
| **Online** | Diccionario español y traducción automática de textos nuevos; idioma configurable | Necesaria para traducir textos nuevos | `ScheduleTranslate.dll` |

En Online, los textos se traducen conforme aparecen en el juego. En español se aprovecha el diccionario integrado; los otros idiomas parten del texto inglés. Las traducciones obtenidas se guardan en una caché por idioma y se reutilizan en futuras partidas.

### Instalación

Necesitas **Schedule I (IL2CPP)** y **[MelonLoader](https://github.com/LavaGang/MelonLoader/releases)** instalado. La compatibilidad con nuevas actualizaciones del juego debe comprobarse.

1. Cierra el juego.
2. Descarga **un solo ZIP**, Online u Offline, desde [Releases](https://github.com/SasoIkuyo/schedule-i-translate/releases).
3. Retira las otras DLL de traducción activas y la versión anterior de este mod.
4. Extrae el ZIP y copia su carpeta `Mods` en la carpeta del juego, junto a `Schedule I.exe`.
5. Inicia el juego normalmente.

Instala **solo una variante**. Los ZIP contienen únicamente la DLL del mod; MelonLoader se instala por separado. No necesitas ejecutar scripts ni PowerShell.

**Al actualizar:** conserva `UserData/ScheduleISpanish` para mantener tus ajustes y traducciones guardadas. Si tienes el prototipo antiguo, retira también `ScheduleSpanishOffline.dll`.

### Configuración

Los archivos de configuración se crean al iniciar el mod. Cierra el juego antes de editarlos y reinícialo para aplicar los cambios.

#### Idioma de Online

Edita `UserData/ScheduleISpanish/language.json`. Por ejemplo, para francés:

```json
{
  "TargetLanguage": "fr"
}
```

| Código | Idioma | Código | Idioma |
| --- | --- | --- | --- |
| `es` | Español, predeterminado | `it` | Italiano |
| `fr` | Francés | `ja` | Japonés |
| `de` | Alemán | `ko` | Coreano |
| `pt` | Portugués | `zh-CN` | Chino simplificado |
| `en` | Inglés original, sin consultas | | |

Estos son ejemplos de códigos admitidos por Google Translate. La selección de idioma solo afecta a Online. Cada idioma usa su propio archivo de caché, como `cache.fr.jsonl`, dentro de `UserData/ScheduleISpanish`.

#### Texto del teléfono

El mod utiliza una fuente existente del juego para mejorar la legibilidad y ajusta el espacio de los rótulos e importes. Los campos editables, los desplegables de destinos y el menú de contraoferta conservan el texto nativo del juego. Si una etiqueta contiene caracteres que la fuente nueva no admite, conserva la fuente original.

Para restaurar por completo el aspecto original del texto, edita `UserData/ScheduleISpanish/display.json`:

```json
{
  "PhoneTextScale": 1,
  "UseSdfPhoneText": false
}
```

Los valores predeterminados son `PhoneTextScale: 2` y `UseSdfPhoneText: true`. No se aumenta la resolución de toda la escena ni se instalan fuentes externas. Las mejoras visuales todavía necesitan comprobación dentro del juego.

### Cómo funciona Online

- Envía los textos nuevos a Google desde la conexión de cada jugador.
- Procesa las consultas una por una, con una cola limitada, y guarda los resultados localmente.
- Reutiliza las traducciones guardadas y excluye contadores FPS, versiones, importes aislados y horas aisladas de las consultas.
- No incluye claves API, credenciales del autor ni un proyecto de facturación de Google Cloud.

Google puede limitar o rechazar solicitudes. Si falla la conexión, las traducciones locales siguen disponibles y los textos nuevos pueden permanecer en inglés.

### Cobertura y problemas conocidos

La traducción combina resultados automáticos y correcciones de vocabulario según el contexto del juego. Todavía pueden quedar frases dinámicas o textos dentro de imágenes sin traducir. Algunas fuentes no incluyen los caracteres de ciertos idiomas.

No se ha verificado la cobertura completa de todos los idiomas ni el funcionamiento en todas las versiones del juego o equipos. Los ajustes de distribución del teléfono y de las opciones de diálogo siguen pendientes de validación visual.

### Reportar un problema

Abre un [issue](https://github.com/SasoIkuyo/schedule-i-translate/issues) e incluye:

- Versión del juego y variante del mod: Online u Offline.
- Idioma elegido y captura o texto afectado.
- `MelonLoader/Latest.log` si hay errores de carga o cierres. Revisa el registro y elimina datos personales antes de compartirlo.

### Código fuente y apoyo

El código y las pruebas están en la rama [source](https://github.com/SasoIkuyo/schedule-i-translate/tree/source). Las instrucciones para compilar están en [BUILD.md](https://github.com/SasoIkuyo/schedule-i-translate/blob/source/BUILD.md).

Si te sirve la traducción, puedes apoyar su desarrollo en [Buy Me a Coffee](https://buymeacoffee.com/sasoikuyo).

## English

Translation for **Schedule I (IL2CPP)** using **[MelonLoader](https://github.com/LavaGang/MelonLoader/releases)**, with offline Spanish and automatic translation into configurable languages while you play.

### Choose a version

| Version | Features | Connection | File |
| --- | --- | --- | --- |
| **Offline** | 12,295 embedded Spanish entries | Not required | `ScheduleISpanish.dll` |
| **Online** | Spanish dictionary, translation of new text and configurable target language | Required for new translations | `ScheduleTranslate.dll` |

Online translates labels as they appear. Spanish uses the embedded dictionary; other languages start from the English text. Results are saved in a separate cache for each language and reused across sessions.

### Installation and updates

1. Install **[MelonLoader](https://github.com/LavaGang/MelonLoader/releases)** for your IL2CPP game installation.
2. Close the game and download **one ZIP** from [Releases](https://github.com/SasoIkuyo/schedule-i-translate/releases).
3. Remove other active translation DLLs and the previous version of this mod.
4. Extract the ZIP and copy its `Mods` folder next to `Schedule I.exe`.
5. Start the game normally.

Install **only one variant**. Packages contain only the mod DLL; no scripts or PowerShell are needed. Keep `UserData/ScheduleISpanish` when updating to preserve settings and cached translations. Remove `ScheduleSpanishOffline.dll` if you used the old prototype. Compatibility with new game updates needs verification.

### Configuration

Launch the mod once to create its settings, then close the game before editing them. Restart to apply changes.

**Online language:** edit `UserData/ScheduleISpanish/language.json`:

```json
{
  "TargetLanguage": "fr"
}
```

Examples: `es` Spanish (default), `fr` French, `de` German, `pt` Portuguese, `it` Italian, `ja` Japanese, `ko` Korean, `zh-CN` Simplified Chinese, or `en` for original English without translation requests. Use a language code supported by Google Translate. Caches such as `cache.fr.jsonl` are stored in `UserData/ScheduleISpanish`.

**Phone text:** the mod uses an existing game font and adjusts label spacing. Editable fields, destination dropdowns and the counteroffer menu retain native game text. Unsupported characters retain the original font. To restore the original appearance, edit `UserData/ScheduleISpanish/display.json`:

```json
{
  "PhoneTextScale": 1,
  "UseSdfPhoneText": false
}
```

Defaults are `PhoneTextScale: 2` and `UseSdfPhoneText: true`. No external fonts or scene resolution changes are included. Visual results still need in-game checking.

### Network and coverage

Online sends new text to Google using each player's connection. Requests run one at a time with a bounded queue and persistent caching. FPS counters, version strings, standalone currency and clock values are excluded. No API keys, author credentials or Google Cloud billing project are embedded.

The service may limit or reject requests. Local translations remain available when the connection fails; new text may stay in English. Translations combine automatic results and contextual corrections. Dynamic text, text inside images and unsupported font characters remain possible gaps. Full language coverage, visual layout and compatibility with every game version or computer have not been verified.

### Reports, source and support

Report problems through [Issues](https://github.com/SasoIkuyo/schedule-i-translate/issues), including the game version, mod variant, language and affected text or screenshot. Attach `MelonLoader/Latest.log` for crashes or load errors, removing personal information first.

Source and tests: [source branch](https://github.com/SasoIkuyo/schedule-i-translate/tree/source) · [Build instructions](https://github.com/SasoIkuyo/schedule-i-translate/blob/source/BUILD.md) · [Support development ☕](https://buymeacoffee.com/sasoikuyo)

## Historial de cambios / Changelog

| Versión / Version | Cambios principales | Main changes |
| --- | --- | --- |
| [1.4.7](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.7) | Controles nativos en Mod Manager para idioma, texto del teléfono, actualizaciones y aviso. | Native Mod Manager controls for language, phone text, updates and notice appearance. |
| [1.4.6](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.6) | Nombres de lugares protegidos, botones + y - nativos, aviso transparente, actualización desde el juego y JSON para Mod Manager. | Protected place names, native + and - buttons, translucent notice, in-game updates and Mod Manager JSON settings. |
| [1.4.5](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.5) | 533 traducciones nuevas integradas y comprobación de actualizaciones. | 533 new embedded translations and update checking. |
| [1.4.4](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.4) | Números romanos protegidos en **todos los idiomas**, con rechazo de caché incorrecta. Los nombres de rango se traducen localmente en español y mediante Google en los demás idiomas. | Roman numerals protected in **all languages**, with invalid-cache filtering. Rank names use local Spanish translations or Google for other languages. |
| [1.4.3](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.3) | Reutilización del texto visual de filas clonadas; desplegables con texto nativo. Avisos de destino y espera traducidos. | Reuse cloned text mirrors; native dropdown rendering. Translated destination and waiting prompts. |
| [1.4.2](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.2) | Actualización de texto visual al cambiar valores; contraoferta y campos editables con texto nativo. Botones «Sí» y «Contraoferta». | Refresh phone mirrors when values change; native counteroffer and editable fields. Corrected Spanish buttons. |
| [1.4.1](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.1) | Espacios separados para opciones y requisitos; avisos de región cortos y mayúsculas corregidas. | Separate option and requirement bounds; shorter region notices and corrected capitalization. |
| [1.4.0](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.4.0) | Fuente del teléfono, ajuste de pedidos y vocabulario de entregas. | Phone font, supplier order fitting and delivery terminology. |
| [1.3.2](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.3.2) | Filtro de FPS, reintentos de consultas y traducción de patinetas con precios. | FPS filtering, request retries and priced skateboard labels. |
| [1.3.1](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.3.1) | Distribución del menú de partidas, tiempo restante y mensajes dinámicos del teléfono. | Save menu layout, remaining time and dynamic phone messages. |
| [1.3.0](https://github.com/SasoIkuyo/schedule-i-translate/releases/tag/v1.3.0) | Bloques de misiones, diálogos variables y correcciones de cultivo y nombres. | Mission blocks, variable dialogue and cultivation/name corrections. |


## 1.4.5

533 traducciones adicionales integradas (12.295 entradas) y aviso opcional de nuevas versiones. / 533 additional embedded Spanish translations (12,295 entries) and update notifications.

## Otros ajustes en Mod Manager / More Mod Manager settings

Desde la **1.4.7**, selecciona nuestro mod en **Mods > Config** o en la aplicación de ajustes del teléfono. Aparecen campos e interruptores normales, sin tener que editar JSON:

- **Idioma / Language** (solo Online): `TargetLanguage`, por ejemplo `es`, `en`, `fr`, `de`, `pt` o `zh-CN`. Requiere reiniciar.
- **Texto del teléfono / Phone text**: activar la fuente nítida (`UseSdfPhoneText`) y elegir nitidez 1 o 2 (`PhoneTextScale`). Requiere reiniciar.
- **Actualizaciones y aviso / Updates and notice**: buscar novedades, actualizar desde el juego, descargar automáticamente, mostrar el aviso, ajustar opacidad/tamaño y activar la vista previa. Se aplica durante la sesión.

Los valores se sincronizan con `language.json`, `display.json` y `updates.json`, en `UserData/ScheduleISpanish`. Los archivos JSON siguen funcionando sin Mod Manager; se conservan los ajustes actuales y los campos adicionales. Los valores no válidos se rechazan y se recupera el último valor válido. La opción Reset de Mod Manager restaura los valores predeterminados de los controles. La protección de nombres y los botones originales del juego permanecen activos.

**English:** Version 1.4.7 adds native Mod Manager controls for Online target language, phone font/sharpness, update checking/downloading and notice appearance. Language and phone changes require restarting; update-notice options apply during the session. Controls sync with the existing JSON files, preserve additional fields and reject invalid values. Mod Manager is optional; JSON configuration still works independently.

## Actualizaciones y Mod Manager / Updates and Mod Manager

Desde la 1.4.6, el aviso tiene fondo semitransparente y un botón **Actualizar**: descarga el ZIP oficial, comprueba SHA-256, variante y versión de la DLL. Cuando cierres el juego, un instalador oculto reemplaza la DLL de forma atómica y conserva la anterior en `UserData/ScheduleISpanish/Updates/<variante>/previous.dll`. La versión nueva se carga al abrir el juego otra vez. No es necesario abrir GitHub ni ejecutar scripts manualmente; solo distribuyes nuestra DLL. El mod genera internamente un script de PowerShell para instalarla después del cierre. No cierra el juego ni toca partidas, ajustes o cachés.

La consulta se realiza una vez al iniciar e incluye versiones preliminares publicadas con ZIP para tu variante. Offline traduce sin conexión: su consulta de actualizaciones está desactivada por defecto. Para pasar de la 1.4.5 a la 1.4.6 hay que instalar la DLL manualmente una vez; la actualización desde el juego funciona a partir de la 1.4.6.

Configuración: `UserData/ScheduleISpanish/updates.json`. Si está instalado **Mod Manager & Phone App 2.2.x**, el mod registra esta ruta JSON; desde la 1.4.7 también muestra controles nativos. Puedes editar los JSON manualmente si lo prefieres. Mod Manager es opcional. Los ajustes del aviso se recargan cada dos segundos; si el JSON no es válido, se conservan los últimos ajustes válidos.

```json
{
  "CheckForUpdates": true,
  "PreviewNotification": false,
  "AllowInGameUpdates": true,
  "AutoDownloadUpdates": false,
  "ShowUpdateNotice": true,
  "NoticeOpacity": 0.58,
  "NoticeScale": 1.0
}
```

| Ajuste / Setting | Efecto / Effect |
| --- | --- |
| `CheckForUpdates` | Consulta al iniciar; `true` en Online, `false` en Offline por defecto. / Startup check; Online default true, Offline default false. |
| `AllowInGameUpdates` | Activa el botón Actualizar; si es false, abre la descarga manual. / Enable in-game installation; false opens the download page. |
| `AutoDownloadUpdates` | Descarga sin pulsar el botón cuando hay novedades; requiere las dos opciones anteriores. Predeterminado false. Instala al cerrar. / Download automatically when an update is found; requires checking and in-game updates. Default false; installs after exit. |
| `ShowUpdateNotice` | Mostrar u ocultar el aviso. / Show or hide the notice. |
| `NoticeOpacity` | Opacidad del fondo, de 0.15 a 0.9. / Background opacity from 0.15 to 0.9. |
| `NoticeScale` | Tamaño del aviso, de 0.6 a 1.5. / Notice size from 0.6 to 1.5. |
| `PreviewNotification` | Mostrar un aviso de diseño de prueba; no descarga ninguna versión ficticia. / Show a design preview; never downloads a fictitious version. |

El estado de instalación se guarda en `UserData/ScheduleISpanish/Updates/<variante>/status.txt`. Si falla la verificación o la instalación, la DLL instalada se conserva. Para recuperar una versión anterior, cierra el juego y copia `previous.dll` a `Mods`, renombrándola `ScheduleTranslate.dll` (Online) o `ScheduleISpanish.dll` (Offline).

**English:** Version 1.4.6 adds a translucent update notice and an **Update** button. The official ZIP is downloaded and checked for SHA-256, variant and assembly version. A hidden PowerShell helper waits for game exit, atomically replaces the DLL and keeps a backup. The game is never closed automatically. Saves, settings and translation caches are preserved. Install 1.4.6 manually once to enable future in-game updates. JSON settings are registered with the optional Mod Manager & Phone App 2.2.x editor and reloaded every two seconds. Invalid edits retain the last valid settings. Offline checks remain disabled by default; automatic downloading is opt-in.
