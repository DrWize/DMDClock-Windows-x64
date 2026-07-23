# DMDClock for Windows x64 — TODO

The goal is to create a standalone Windows application that displays a clock and plays DotClk animations on a regular monitor. The application must not require a Raspberry Pi, Teensy, or physical DMD hardware.

## Active scope

Active development covers only the classic DotClk display: 128×32, four-bit monochrome `.scn` files, storyboards/masks, clock layers, library management, controls, and classic DMD themes. Serum, cRom, full RGB, larger DMD formats, DMD Extensions integration, and colorization tools are frozen as future work and must not be prioritized or connected to the application now.

Audio is outside the project scope. The application must not play, import, or require audio tracks.

The finished clock should look like a classic DMD: a 128×32-dot surface where digits, text, and images consist of clearly separated, round glowing dots against a black background. The default appearance should use orange/red-yellow dots with varying intensity, subtle glow, and visible dark space between dots—not smooth fonts or ordinary display pixels. The display must retain its 4:1 aspect ratio and a thin black border at every scale.

## Active work

Only unfinished work is listed in this part of the document. Completed work is archived at the end.

## Standalone EXE/SCR roadmap

The target is a standalone `DmdClock.App.exe` and a separate standalone `DMDClock.scr` that require neither adjacent DLL files nor an installed .NET runtime. Translations, downloaded `.scn` animations, and user-supplied fonts remain external resources so they can be installed or updated independently.

### Standalone resources

- [ ] Provide a clear fallback and error message when external translations are unavailable

### Standalone publishing profile

- [ ] Investigate trimming only after the untrimmed single-file build is fully verified

### Standalone binary generation

- [ ] Verify `/s`, `/c`, and `/p <HWND>` screensaver modes
- [ ] Verify installation and selection through Windows Screen Saver Settings

### Standalone writable-data handling

- [ ] Avoid requiring write access beside the executable
- [ ] Verify operation when the executable is launched from a read-only directory

### Standalone release validation

- [ ] Test normal EXE startup, clock display, localization, fonts, menus, and SCN playback
- [ ] Test the screensaver in fullscreen, configuration, and Control Panel preview modes
- [ ] Test on clean Windows 10 and Windows 11 x64 systems without .NET installed
- [ ] Check native-library extraction, cleanup, startup time, and package size
- [ ] Check Windows SmartScreen and antivirus behavior

### Standalone completion criteria

- [ ] The screensaver works through Windows Screen Saver Settings
- [ ] Clean-machine testing succeeds on Windows 10 and Windows 11 x64

## Decisions

### Appearance

- [ ] Add scrolling C64-inspired palettes/raster bars with selectable direction and speed, a slow default, and an option to disable animation
- [ ] Future: animated colors
- [ ] Future: selectable classic monochrome DMD or full color

### Display

- [ ] Fullscreen on a selected monitor
- [ ] Start automatically with Windows
- [ ] Always on top
- [ ] Show a clear but discreet paused indicator

### Clock

- [ ] Optional date display
- [ ] Display weekday
- [ ] Add weather later

### Animation selection and playback order

- [ ] Random selection from all enabled Stern animations
- [ ] Random selection within one selected game
- [ ] Sequential playback within one selected game
- [ ] Chronological playback according to original file/sequence order
- [ ] Make manufacturers selectable, such as Stern, Williams, and Bally, when the material can be identified reliably
- [ ] Display a library tree: manufacturer → game → animation
- [ ] Checkbox to enable or disable a complete manufacturer
- [ ] Checkbox to enable or disable a complete game
- [ ] Checkbox to enable or disable one animation
- [ ] Add search plus `Select all`, `Clear all`, and `Reset`
- [ ] Show a preview and basic details for the selected animation
- [ ] Preserve all selections and blocked animations between application starts
- [ ] Find or create a curated SCN list with consistent descriptive filenames and associated game/scene metadata

### Updatable animation library

- [ ] Add new files without resetting enablement, block lists, or playlists

## Work plan

### 2. Basic Windows application

- [ ] Support development, build, and debugging directly in VS Code
- [ ] Avoid dependencies on full Visual Studio and project-specific user settings
- [ ] Add recommended VS Code extensions to `.vscode/extensions.json`
- [ ] Add build, test, and publish commands to `.vscode/tasks.json`
- [ ] Add launch and debugging to `.vscode/launch.json`
- [ ] Verify that the same commands work in VS Code and directly in PowerShell

### 3. Clock functionality

- [ ] Implement every mode under `Animation selection and playback order`

### 4. Settings

- [ ] Select monitor
- [ ] Set frame rate
- [ ] Enable or disable clock, date, and animations
- [ ] Select playback mode: all, manufacturer, game, random, or chronological
- [ ] Manage enabled and disabled manufacturers, games, and individual animations

### 4c. Raspberry Pi and ESP32-S3

- [ ] Build Windows and Raspberry Pi versions from the same cross-platform core
- [ ] Define and document a versioned `DmdFrame` format independent of C# and ESP-IDF
- [ ] Create shared test vectors verified identically by the desktop application and ESP32 firmware
- [ ] Create a conversion tool that builds optimized versioned ESP32 animation packages from current library files
- [ ] Add a manifest with file ID, content hash, format version, and package version to every ESP32 package
- [ ] Make ESP32 packages easy to replace by SD card and prepare safe local-network updates
- [ ] Fully validate a package before activation and retain the previous valid package after errors or interrupted transfers
- [ ] Include new Windows/Raspberry Pi animation files through an incremental package build without changing firmware

### 5. Distribution

- [ ] Consider a conventional installer
- [ ] Add a shortcut and optional automatic startup
- [ ] Test on a clean Windows 10/11 computer

### 6. README and documentation

- [ ] Explain how to open and use the project in VS Code
- [ ] List required and recommended VS Code extensions
- [ ] Show how to run `Get-OriginalResources.ps1`
- [ ] Describe testing one file and the complete animation collection
- [ ] Add troubleshooting for missing resources, invalid `.scn` files, and display problems

## Important checks

- [ ] License assessments are handled separately and should not block technical development

## Ideas for later versions

### Future — Serum, full color, and larger DMDs

- [ ] Investigate GPL-2.0 and select a safe integration boundary before importing code or binaries
- [ ] Make general frame size, stride, and pixel/color formats explicit in a versioned `DmdFrame`
- [ ] Add indexed color and RGB24 without affecting monochrome playback
- [ ] Test 128×32, 192×64, and 256×64 using fit, fill, stretch, and integer scaling
- [ ] Investigate Serum, cRom, VNI, PAL, and PAC colorization
- [ ] Implement dynamic palette sets, area masks, backgrounds, color rotations, and sprites
- [ ] Add license and author metadata for each locally added colorization
- [ ] Verify future color rendering against DMD Extensions in a separate integration-test profile
- [ ] Investigate network adapters and physical DMD output only after the classic display is complete
- [ ] Custom named playlists in addition to built-in selection modes
- [ ] Favorites in addition to the required enable/block list
- [ ] Day/night brightness schedule
- [ ] DMD effects such as glow, scanlines, and color palettes
- [ ] GIF and MP4 support in addition to `.scn`
- [ ] Mobile remote control through a local web interface
- [ ] Physical LED matrix or Pin2DMD support

## Additional feature ideas

### Clock and automatic display

- [ ] Multiple clock layouts with or without seconds and date
- [ ] Display seconds as digits, a blinking colon, or not at all
- [ ] Selectable transitions between clock and animation: immediate, fade, or DMD dissolve
- [ ] Schedule when the display is active, dimmed, or completely black
- [ ] Separate brightness for day and night
- [ ] Prevent burn-in through small position shifts and varied clock layouts
- [ ] Support multiple automatically rotating time zones

### Animations and library

- [ ] Avoid replaying an animation until the others in the selection have been shown
- [ ] Playback history with recently displayed games and animations
- [ ] Temporary `Do not show again` directly from the context menu
- [ ] Minimum and maximum display duration plus repeat count per animation
- [ ] Allow short animations to loop a configured number of times
- [ ] Custom labels and corrections for manufacturer, game, and animation names
- [ ] Thumbnails or first-frame previews in the library
- [ ] Show duplicates and let the user choose which copy to use

### Image and display

- [ ] Selectable dot shape, size, spacing, and glow strength
- [ ] Color palette globally, per manufacturer, or per game
- [ ] Pixel-perfect integer scaling when display size permits
- [ ] Return automatically to the correct monitor if one is disconnected
- [ ] Separate settings for each connected monitor
- [ ] Future: support larger DMD surfaces and full color according to the DMD Extensions plan

### Operation and safety

- [ ] System-tray icon with play/pause, show clock, next animation, and exit
- [ ] Resume the last mode after restart or power failure
- [ ] Export and import settings and enablement lists
- [ ] Back up settings automatically before major updates
- [ ] Library report for valid, damaged, and disabled files
- [ ] Diagnostics page with current file, frame rate, resolution, and decoding errors
- [ ] Silent fullscreen error mode: skip the error and log it without a dialog

## Completed work

Completed items are retained here as the project history.

### Next prioritized work — Priority 1 — play a selected SCN file

- [x] Implement a playback engine that follows storyboard frame delays
- [x] Implement storyboard first/last steps, blanking, transparency masks, and clock layers according to the original firmware
- [x] Add file opening and play a selected `.scn` file in the existing DMD renderer
- [x] Add play/pause, next frame, and previous frame
- [x] Switch from the clock to an animation and back without freezing the UI
- [x] Add tests for timing, pause, completion, and damaged files

### Next prioritized work — Priority 2 — select a directory and keep the library updated

- [x] Use `./scenes` as the default directory, create it when needed, and scan it automatically at startup
- [x] Preserve the published `scenes` directory between builds
- [x] Add animation-directory selection and an initial recursive scan
- [x] Create a versioned atomic library index with stable file IDs
- [x] Detect new, changed, and removed `.scn` files incrementally
- [x] Skip damaged files, log the reason, and continue playing valid files
- [x] Log start time, end time, duration, and result for every library scan
- [x] Log transitions between clock, date, and a named animation
- [x] Log application startup with a unique build ID and graceful exit with uptime
- [x] Limit the active log to 3 MiB and rotate one previous log
- [x] Display and log game plus animation scene as small regular text in the lower-right corner at playback start
- [x] Add random and naturally sorted sequential playback

### Next prioritized work — Priority 3 — basic controls and saved settings

- [x] Implement the context menu with play/pause, next, previous, and show clock
- [x] Save automatic cycle, intervals, animation count, and random/sequential mode in AppData
- [x] Add Space, T, D, I, F11, and Escape keyboard shortcuts
- [x] Add borderless fullscreen
- [x] Save animation directory, playback mode, interval, color, and brightness in AppData

### Next prioritized work — Priority 4 — DotClk fonts

- [x] Make the font selectable and retain the built-in 5×7 font as fallback
- [x] Include an openly licensed TTF font with Swedish characters under `assets/fonts`
- [x] Implement TTF/OTF rendering to a four-bit `DmdFrame`
- [x] Implement and test the original DotClk `.fnt` reader
- [x] Embed ALTERN8, FISHY, TREK, and TWILIGHT in the application
- [x] Make all four DotClk fonts selectable independently for both time and date
- [x] Supply DMD-style fallback date separators without changing the original digit glyphs

### Next prioritized work — Priority 5 — first distributable Windows build

- [x] Complete the README
- [x] Create a self-contained `win-x64` build
- [x] Verify that the previous build is archived before every new build
- [x] Run a complete SCN compatibility scan and store its report with the build
- [x] Create a portable ZIP
- [x] Create standalone single-file Windows `.exe` and `.scr` builds

### Decisions — Technology

- [x] Determine whether Java is needed at all
- [x] Document only the old Java code's `.scn` parsing and behavior
- [x] Replace old Java code with a modern native implementation
- [x] C# with .NET 10 LTS and Avalonia UI for Windows and Raspberry Pi

### Decisions — Appearance

- [x] Classic orange DMD dots
- [x] Thin black border around the dot-matrix display
- [x] No permanent menu bar or visible settings buttons

### Decisions — Display

- [x] Regular movable window
- [x] Borderless window with optional title bar and left-click drag movement
- [x] Optional Windows x64 screensaver (`.scr`) using the same clock, animations, and settings
- [x] Right-click anywhere in the display to open the complete menu
- [x] Close the menu when clicking outside it or pressing Escape
- [x] Keep the menu open after selecting an option so several settings can be changed consecutively
- [x] Display Alien Tech for four seconds at startup and link Help to the GitHub project
- [x] Store menus in external i18n files with English as default, Swedish translation, and a translation template
- [x] Move a borderless window by left-clicking and dragging
- [x] Space pauses or resumes playback
- [x] T displays the time immediately
- [x] D displays the date immediately when date display is enabled
- [x] I toggles the game and scene information overlay
- [x] F11 toggles fullscreen and Escape leaves fullscreen

### Decisions — Clock

- [x] Selectable 24-hour format
- [x] Selectable 12-hour format
- [x] Optional seconds
- [x] Common selectable date formats: ISO, European, US, and dot-separated
- [x] Display time, play a video/animation, and then return to time
- [x] Make the number of videos between clock displays configurable, defaulting to one
- [x] Make clock duration between animation rounds configurable
- [x] Make clock pauses between animations in the same cycle configurable

### Decisions — Animation selection and playback order

- [x] Random selection from all enabled animations
- [x] When reliable time metadata is unavailable, use natural directory/filename sorting and show which order is used
- [x] Read optional `scene-metadata.json` prefix rules and exact file overrides
- [x] Map local `RD####.scn` files to games and scene numbers from `RD Index.txt`
- [x] Skip disabled, missing, or damaged animations without stopping playback
- [x] Display the number of active animations in the current selection

### Decisions — Updatable animation library

- [x] Detect new, changed, moved, and removed files without requiring an application update
- [x] Provide automatic watching and manual `Rescan` from the menu
- [x] Use file size, modification time, and content hash for incremental rescans so unchanged files are not decoded again
- [x] Keep the library usable during a large rescan and display discreet status plus the final result
- [x] Use stable library IDs and preserve IDs after content updates or moves when the file can be identified
- [x] Handle files still being copied by detecting changes during scanning and retrying on the next file event
- [x] Write the library index atomically and retain the last valid index when a scan is canceled or fails
- [x] Version the index format; add migration when a second schema version exists
- [x] Report new format versions and unknown files without stopping playback of known files

### Work plan — 1. Investigate the file format

- [x] Download DotClk Resources from GitHub
- [x] Document the `.scn` format structure
- [x] Compare parsing with the Modern Hackerspace Java code
- [x] Determine which Java behavior must be reimplemented and what can be omitted
- [x] Implement the `.scn` reader directly in the selected modern platform
- [x] Verify frame size, color depth, and frame delays
- [x] Create tests with a few small animation files
- [x] Create an automatic compatibility scan for the complete animation collection
- [x] Verify that every `.scn` file can be opened and decoded without crashing
- [x] Log files with unknown versions, damaged frames, or invalid timing values
- [x] Produce a report with counts of accepted, warned, and rejected files

### Work plan — 1b. Fonts

- [x] Check which letters, digits, and symbols each font contains
- [x] Add a built-in default font as fallback
- [x] Document the `.fnt` format
- [x] Verify that all four fonts can be read and displayed correctly
- [x] Display available fonts in the clock and date context menus
- [x] Report newly added fonts during resource validation

### Work plan — 2. Basic Windows application

- [x] Create a .NET 10 project targeting Windows x64
- [x] Create a regular `.sln` and clearly organized `.csproj` projects
- [x] Create a main window with a 4:1 aspect ratio
- [x] Implement the first DMD renderer using separated round dots
- [x] Add a thin black border around the display
- [x] Implement the complete context menu in the display
- [x] Add an open-directory dialog
- [x] Add start, stop, next, and previous animation
- [x] Display clear error messages for damaged or unknown files

### Work plan — 3. Clock functionality

- [x] Create a built-in DMD-friendly numeric font
- [x] Display the current time
- [x] Render each DMD dot as a clearly separated round light against a black background
- [x] Preserve 128×32 resolution and 4:1 aspect ratio at every scale
- [x] Add optional glow and brightness without merging adjacent dots
- [x] Return automatically to the clock when an animation completes
- [x] Add settings for clock interval and animations per cycle

### Work plan — 4. Settings

- [x] Make implemented settings available from the context menu
- [x] Select animation directory
- [x] Select window size or fullscreen
- [x] Select DMD color and brightness
- [x] Select 12- or 24-hour format
- [x] Select animations between clock displays
- [x] Save implemented settings under the user's AppData directory

### Work plan — 4b. Local original resources

- [x] Store resources in a local Git-ignored `external/` directory
- [x] Never commit original animations, external binaries, or external projects
- [x] Create `scripts/Get-OriginalResources.ps1`
- [x] Download required original resources from their official GitHub and GitLab locations
- [x] Make the script safe to run repeatedly without duplicates
- [x] Add options to update or download resources again
- [x] Show new, changed, and removed content before or after a resource update
- [x] Store local version, commit, or download information for reproducible tests
- [x] Provide clear errors for network failures or changed download locations
- [x] Keep the application functional without resources and explain how to obtain them

### Work plan — 5. Distribution

- [x] Use one build script that always archives the previous build before replacing `output/current`
- [x] Close related DMDClock processes before building and start the new Windows build after successful publication
- [x] Store old builds under `output/archive/<timestamp>-<platform>` with a manifest
- [x] Retain the 10 newest archives automatically, with a configurable limit
- [x] Publish the current Windows x64 distribution
- [x] Create a self-contained version that requires no separate .NET installation
- [x] Create a portable ZIP

### Work plan — 6. README and documentation

- [x] Create a structured `README.md`
- [x] Explain the project purpose and what is not included in the repository
- [x] Show how to build for Windows x64
- [x] Document bundled fonts, origin, and license
- [x] Document restore, build, test, and publish commands
- [x] Show where local animations and fonts belong
- [x] Explain how to start and control the application
- [x] Document the context menu and keyboard shortcuts
- [x] Link to `docs/SOURCES.md` for persistent original-resource and reference links

### Important checks

- [x] The GitHub repository must not contain proprietary animations, ROM files, or external binary data
- [x] The GitHub repository should contain only our source code, documentation, and freely distributable test resources
- [x] Let users select a local animation directory outside the application and repository
- [x] Use synthetic or project-created minimal `.scn` files in automated tests
- [x] Retain technical source references for projects whose file formats or behavior were studied
- [x] Original resources may be linked from the README, documentation, and download scripts
- [x] Preserve all user-provided original links in `docs/SOURCES.md`
- [x] Do not run old Java tools in the final product

### Ideas for later versions — Future — Serum, full color, and larger DMDs

- [x] Add DMD Extensions as a persistent technical reference
- [x] Document the ColorizingDMD guide's Serum workflow as a future reference
- [x] Preserve the isolated prototype for synthetic dumps, six-bit palettes, mask matching, and monochrome fallback as dormant reference code
- [x] Windows screensaver mode

### Additional feature ideas — Animations and library

- [x] Detect new, changed, and removed files during rescans

### Additional feature ideas — Image and display

- [x] Quick presets for classic orange, red, plasma, and monochrome
- [x] Hide the mouse cursor after five seconds of inactivity over the display

### Additional feature ideas — Outside project scope

- [x] No audio playback or audio handling

### Standalone EXE/SCR roadmap — Standalone resources

- [x] Keep `i18n/*.json` outside the executable and load translations from an external `i18n` directory
- [x] Keep downloaded `.scn` animations outside the executable and standalone package
- [x] Document where translations and `.scn` resources can be downloaded and installed
- [x] Retain support for optional user-supplied `.ttf` and `.otf` files
- [x] Verify that missing optional scenes and fonts do not prevent startup
- [x] Embed the four bundled DotClk clock fonts and load them from application resources

### Standalone EXE/SCR roadmap — Standalone publishing profile

- [x] Publish self-contained for `win-x64` with `PublishSingleFile=true`
- [x] Bundle native libraries with `IncludeNativeLibrariesForSelfExtract=true`
- [x] Enable single-file compression and measure startup time plus file size
- [x] Exclude `.pdb` debugging symbols from distributable builds
- [x] Keep trimming disabled initially and document the Avalonia reflection constraint

### Standalone EXE/SCR roadmap — Standalone binary generation

- [x] Publish one standalone `DmdClock.App.exe`
- [x] Copy the verified standalone executable to `DMDClock.scr`

### Standalone EXE/SCR roadmap — Standalone writable-data handling

- [x] Verify settings and library indexes are stored under `%LOCALAPPDATA%\DmdClock`
- [x] Allow scenes and user fonts to be loaded from user-selected directories
- [x] Continue supporting portable `scenes` and `fonts` directories when their location is writable

### Standalone EXE/SCR roadmap — Standalone packaging

- [x] Extend `scripts/Build.ps1` to create `output/current/win-x64-standalone`
- [x] Include `DmdClock.App.exe`, `DMDClock.scr`, external `i18n`, `README.md`, `build-info.json`, and `SHA256SUMS.txt`
- [x] Exclude downloaded `.scn` animations from the standalone package
- [x] Create a separate standalone portable ZIP without runtime DLL files
- [x] Archive the previous standalone build before replacement
- [x] Generate and verify SHA-256 checksums automatically

### Standalone EXE/SCR roadmap — Standalone release validation

- [x] Run all automated tests against the standalone build workflow

### Standalone EXE/SCR roadmap — Completion criteria

- [x] Both standalone files run without adjacent DLL files
- [x] Neither standalone file requires an installed .NET runtime
- [x] Settings and user-selected resources persist correctly
- [x] The standalone package and checksums are generated automatically
