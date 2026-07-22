# DMDClock for Windows

DMDClock is a self-contained Windows x64 clock and animation player for classic DotClk `.scn` scenes. It recreates a 128×32, 4-bit monochrome dot-matrix display in a scalable Avalonia window, with clock overlays, scene metadata, automatic library updates and configurable playback.

The active project scope is the original single-color DMD format. Full-color Serum, cRom, larger displays and DMD Extensions integration are documented as future work.

## Screenshots

### Time

![DMDClock showing the time](docs/screenshots/time.png)

### Date

![DMDClock showing an ISO-formatted date](docs/screenshots/date.png)

## Features

- Classic 128×32 orange, red, plasma or monochrome DMD appearance
- DotClk `.scn` playback with storyboard timing, masks, blanking and clock layers
- Automatic clock/animation cycles with configurable duration, count and gaps
- Sequential or random playback
- 12-hour clock with AM/PM or 24-hour clock, with optional seconds
- ISO, European, US and dot-separated date formats
- Recursive `./scenes` library with incremental rescanning and file watching
- Optional `scene-metadata.json` for game, manufacturer and sequence information
- Five-second game/sequence overlay at animation start
- English menus by default, Swedish translation and a reusable i18n template
- Startup branding with the actual number of successfully loaded animations
- Keyboard shortcuts, fullscreen mode and a persistent right-click menu
- Optional title bar with click-and-drag movement in borderless mode
- Structured UTF-8 logs with a 3 MiB rotation limit

## Running a published build

1. Open `DmdClock.App.exe` from the published `win-x64` directory.
2. Put `.scn` files in the `scenes` directory beside the executable.
3. Restart the app or choose **Rescan library (F5)**.

Scene files are intentionally not included in this repository. The application creates or uses `./scenes` by default, scans subdirectories recursively and preserves that directory across local builds.

Put `.ttf` or `.otf` files anywhere under the `fonts` directory beside the executable. Open the menu and choose **Appearance → Clock → Font** or **Appearance → Date → Font**; the font list is refreshed whenever the menu opens. Font choices and user-added font files are preserved across local builds. The built-in 5×7 font remains available as a fallback.

## Building from source

Requirements:

- Windows 10 or Windows 11 x64
- .NET 10 SDK
- PowerShell 7 recommended

```powershell
dotnet test DmdClock.sln -c Release
./scripts/Build.ps1
```

`Build.ps1` closes a running DMDClock instance, archives the previous published build, publishes a self-contained Windows x64 build, copies the local scene library, generates compatibility/build reports and starts the new executable. Use `-NoStart` when an automatic launch is not wanted.

Published files are written to:

```text
output/current/win-x64/
```

Previous builds are retained under `output/archive/`.

The application and window icon use the selected single-flipper concept number 3. The multi-resolution Windows icon and its 512 px source are stored in [`assets/icons`](assets/icons).

Under **Appearance**, foreground and background colors can be selected with an RGB/hex color picker. Custom colors are saved in AppData; choosing one of the existing color themes resets the foreground to that theme while retaining the selected background.

## Controls

Right-click anywhere on the display to open the full menu. The menu remains open while changing options and closes when clicking outside it.

| Shortcut | Action |
| --- | --- |
| `Space` | Play or pause |
| `T` | Show the clock |
| `D` | Show the date |
| `I` | Toggle game/sequence information |
| `N` / `P` | Next / previous animation |
| `Left` / `Right` | Previous / next frame |
| `F5` | Rescan the scene library |
| `F11` | Toggle fullscreen |
| `Escape` | Leave fullscreen or close the menu |
| `Ctrl+O` | Open one SCN file |
| `Ctrl+Shift+O` | Choose a scene directory |

## Scene metadata

Place `scene-metadata.json` in the active scene directory to associate filename prefixes or exact files with game information. When metadata is unavailable, DMDClock falls back to the SCN filename.

See [Scene metadata](docs/SCENE-METADATA.md) and the [SCN format notes](docs/SCN-FORMAT.md) for details.

## Translation

English is the fallback and default language. Translation files are stored in [`assets/i18n`](assets/i18n):

- `en.json` – English
- `sv.json` – Swedish
- `template.json` – documented template for additional languages

Copy the template to an ISO 639-1 filename such as `de.json`, translate the empty values and add the language to the Language menu. The `_comment_*` entries explain each group.

## Logs and settings

Runtime data is stored under:

```text
%LOCALAPPDATA%\DmdClock\
```

- `settings.json` contains saved preferences.
- `library-index.json` contains the incremental scene index.
- `logs/dmdclock.log` is the active structured text log.
- `logs/dmdclock.log.previous` is the previous rotated log.

The active log is limited to 3 MiB. Startup, graceful exit, scans, display changes, game metadata and build IDs are recorded.

## Documentation and roadmap

- [Project TODO](TODO.md)
- [SCN format notes](docs/SCN-FORMAT.md)
- [Scene metadata](docs/SCENE-METADATA.md)
- [Future DMD Extensions work](docs/FUTURE-DMD-EXTENSIONS.md)
- [Source references](docs/SOURCES.md)

Planned work includes packaging, richer library selection, `.fnt` support, Raspberry Pi validation, ESP32-S3 research and an optional Windows screensaver mode.

## Font attribution

The bundled Inter font is licensed under the SIL Open Font License 1.1. See [`assets/fonts/Inter/OFL-1.1.txt`](assets/fonts/Inter/OFL-1.1.txt).

### Optional Pinball font family

The five-style **Pinball** OpenType family has been downloaded separately for design evaluation from [Fontalicious](https://www.fontalicious.com/fonts/pinball):

- Pinball
- Pinball Galaxy
- Pinball Scrambler
- Pinball Scrambler II
- Pinball Wizard

Fontalicious labels the family as a free download and requests contact for commercial use. The `.otf` files are therefore not committed to this repository or included in published builds unless redistribution rights are confirmed. If the user places them in the installed `fonts` directory, DMDClock can render them into the 128×32 four-bit DMD frame.
