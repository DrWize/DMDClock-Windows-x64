using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using DmdClock.Core;
using DmdClock.App.Logging;
using DmdClock.App.Localization;
using DmdClock.App.Rendering;
using DmdClock.Core.Clock;
using DmdClock.Core.Library;
using DmdClock.Core.Playback;
using DmdClock.Core.Rendering;
using DmdClock.Core.Scn;
using DmdClock.Core.Settings;

namespace DmdClock.App;

public partial class MainWindow : Window
{
    private static readonly TimeSpan AnimationInformationDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StartupBrandDuration = TimeSpan.FromSeconds(2);
    private const string HelpGitHubUrl = "https://github.com/DrWize/DMDClock-Windows-x64";
    private readonly DispatcherTimer _displayTimer;
    private readonly AnimationLibraryScanner _libraryScanner = new();
    private readonly AnimationLibraryStore _libraryStore = new();
    private readonly SceneMetadataStore _metadataStore = new();
    private readonly SemaphoreSlim _scanGate = new(1, 1);
    private readonly string _indexPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DmdClock", "library-index.json");
    private readonly AppFileLogger _log = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DmdClock", "logs", "dmdclock.log"));
    private readonly DmdClockSettingsStore _settingsStore = new();
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DmdClock", "settings.json");
    private readonly List<AnimationLibraryItem> _playableItems = [];
    private readonly Dictionary<MenuItem, string?> _clockFontItems = [];
    private readonly Dictionary<MenuItem, string?> _dateFontItems = [];
    private readonly DateTimeOffset _startedUtc = DateTimeOffset.UtcNow;
    private readonly string _buildId = GetBuildId();
    private ScenePlaybackSession? _playback;
    private AnimationLibraryIndex? _libraryIndex;
    private SceneMetadataCatalog _sceneMetadata = SceneMetadataCatalog.Empty;
    private FileSystemWatcher? _libraryWatcher;
    private CancellationTokenSource? _rescanCancellation;
    private CancellationTokenSource? _informationCancellation;
    private readonly CancellationTokenSource _startupBrandCancellation = new();
    private DisplayMode _displayMode = DisplayMode.Time;
    private WindowState _windowStateBeforeFullscreen = WindowState.Normal;
    private DateTimeOffset _lastClockRender = DateTimeOffset.MinValue;
    private DateTimeOffset _clockUntilUtc = DateTimeOffset.MinValue;
    private DateTimeOffset? _nextAnimationAtUtc;
    private string? _libraryRoot;
    private int _libraryPosition = -1;
    private bool _isPaused;
    private bool _randomMode;
    private bool _automaticStartInProgress;
    private bool _startupBrandVisible = true;
    private bool _exitRequestedByMenu;
    private int _animationsRemainingInCycle;
    private string? _status;
    private string? _lastLoggedDisplay;
    private DmdClockSettings _settings = DmdClockSettings.Default;

    public MainWindow()
    {
        InitializeComponent();
        LogStartup();
        _displayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _displayTimer.Tick += (_, _) => Tick();
        Show(DisplayMode.Time);
        _displayTimer.Start();
        Opened += OnOpened;
        Closed += OnClosed;
        Dispatcher.UIThread.Post(() => _ = InitializeDefaultLibraryAsync());
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.O && e.KeyModifiers == KeyModifiers.Control)
            _ = OpenSceneAsync();
        else if (e.Key == Key.O && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            _ = ChooseFolderAsync();
        else
        {
            switch (e.Key)
            {
                case Key.Space: TogglePause(); break;
                case Key.T: Show(DisplayMode.Time); break;
                case Key.D: Show(DisplayMode.Date); break;
                case Key.I: ToggleAnimationInformation(); break;
                case Key.F11: ToggleFullscreen(); break;
                case Key.F5: _ = ScanLibraryAsync(startPlayback: false); break;
                case Key.Right: MoveFrame(1); break;
                case Key.Left: MoveFrame(-1); break;
                case Key.N: _ = PlayLibraryOffsetAsync(1); break;
                case Key.P: _ = PlayLibraryOffsetAsync(-1); break;
                case Key.Escape when WindowState == WindowState.FullScreen:
                    WindowState = _windowStateBeforeFullscreen;
                    break;
                default: return;
            }
        }

        e.Handled = true;
    }

    private void Tick()
    {
        if (_isPaused) return;
        var now = DateTimeOffset.UtcNow;

        if (_displayMode == DisplayMode.Animation && _playback is not null)
        {
            var changed = _playback.Advance(now);
            var localNow = DateTimeOffset.Now;
            if (changed || localNow.Second != _lastClockRender.Second || localNow.Minute != _lastClockRender.Minute)
            {
                _lastClockRender = localNow;
                Display.Frame = RenderPlaybackFrame(localNow);
            }
            if (_playback.IsComplete) HandlePlaybackCompleted();
            return;
        }

        if (_displayMode == DisplayMode.Animation) return;

        if (now.Second != _lastClockRender.Second || now.Minute != _lastClockRender.Minute)
            UpdateClockOrDate();

        if (_nextAnimationAtUtc is not null)
        {
            if (now >= _nextAnimationAtUtc && !_automaticStartInProgress)
            {
                _nextAnimationAtUtc = null;
                _automaticStartInProgress = true;
                _ = ContinueAutomaticCycleAsync();
            }
            return;
        }

        if (_displayMode == DisplayMode.Time && _settings.AutomaticCycle && _playableItems.Count > 0 &&
            now >= _clockUntilUtc && !_automaticStartInProgress)
        {
            _automaticStartInProgress = true;
            _ = BeginAutomaticCycleAsync();
        }
    }

    private async Task OpenSceneAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Öppna DotClk-animation",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("DotClk scene") { Patterns = ["*.scn"] }]
        });
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null) await LoadAndPlayAsync(path);
    }

    private async Task ChooseFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Välj animationsmapp",
            AllowMultiple = false
        });
        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (path is null) return;

        _libraryRoot = Path.GetFullPath(path);
        await ScanLibraryAsync(startPlayback: true);
        StartLibraryWatcher();
    }

    private async Task ScanLibraryAsync(bool startPlayback)
    {
        if (_libraryRoot is null) return;
        await _scanGate.WaitAsync();
        var scanStartedUtc = DateTimeOffset.UtcNow;
        Exception? scanError = null;
        await _log.WriteAsync(scanStartedUtc, $"scan.start root=\"{_libraryRoot}\" startUtc={scanStartedUtc:O}");
        SetStatus("Skannar bibliotek…");
        try
        {
            await LoadSceneMetadataAsync();
            var stored = await _libraryStore.LoadAsync(_indexPath);
            var previous = stored is not null &&
                           string.Equals(stored.RootPath, _libraryRoot, StringComparison.OrdinalIgnoreCase)
                ? stored
                : _libraryIndex;
            _libraryIndex = await Task.Run(() => _libraryScanner.ScanAsync(_libraryRoot, previous));
            await _libraryStore.SaveAtomicAsync(_libraryIndex, _indexPath);

            var currentId = _libraryPosition >= 0 && _libraryPosition < _playableItems.Count
                ? _playableItems[_libraryPosition].Id
                : null;
            _playableItems.Clear();
            _playableItems.AddRange(_libraryIndex.Items.Where(static item => item.IsValid));
            StartupAnimationCountText.Text = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                L("startupLoaded"),
                _playableItems.Count);
            _libraryPosition = currentId is null ? -1 : _playableItems.FindIndex(item => item.Id == currentId);

            var broken = _libraryIndex.Items.Count(static item => !item.IsValid);
            SetStatus($"{_playableItems.Count} animationer" + (broken > 0 ? $", {broken} fel" : string.Empty));
            if (startPlayback && _playableItems.Count > 0) await PlayLibraryOffsetAsync(1);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            scanError = exception;
            SetStatus($"Biblioteksfel: {exception.Message}");
        }
        catch (Exception exception)
        {
            scanError = exception;
            throw;
        }
        finally
        {
            var scanEndedUtc = DateTimeOffset.UtcNow;
            var duration = scanEndedUtc - scanStartedUtc;
            var total = _libraryIndex?.Items.Count ?? 0;
            var valid = _libraryIndex?.Items.Count(static item => item.IsValid) ?? 0;
            var failures = total - valid;
            var status = scanError is null ? "success" : "failed";
            var error = scanError is null ? string.Empty : $" error=\"{SanitizeLogValue(scanError.Message)}\"";
            await _log.WriteAsync(scanEndedUtc,
                $"scan.end status={status} root=\"{_libraryRoot}\" startUtc={scanStartedUtc:O} " +
                $"endUtc={scanEndedUtc:O} durationMs={duration.TotalMilliseconds:F0} " +
                $"files={total} valid={valid} failures={failures}{error}");
            _scanGate.Release();
        }
    }

    private async Task LoadAndPlayAsync(string path, string? relativePath = null)
    {
        CancelInformationDisplay();
        try
        {
            var scene = await Task.Run(() => ScnReader.Read(path));
            var metadata = ResolveSceneMetadata(path, relativePath);
            SetStatus(metadata.DisplayName);

            var now = DateTimeOffset.UtcNow;
            _playback = new ScenePlaybackSession(scene, now);
            if (_isPaused) _playback.Pause(now);
            _displayMode = DisplayMode.Animation;
            Display.Frame = RenderPlaybackFrame(DateTimeOffset.Now);
            LogDisplayedAnimation(path, scene.Frames.Count, metadata);

            if (_settings.ShowAnimationInfo ?? true)
            {
                var sequence = metadata.Title ?? Path.GetFileNameWithoutExtension(metadata.FileName);
                var cancellation = new CancellationTokenSource();
                _informationCancellation = cancellation;
                AnimationGameText.Text = metadata.Game ?? "Okänt spel";
                AnimationSequenceText.Text = $"Sekvens: {sequence}";
                AnimationInfoOverlay.IsVisible = true;
                LogDisplayedInformation(path, metadata, sequence);
                try
                {
                    await Task.Delay(AnimationInformationDuration, cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    if (ReferenceEquals(_informationCancellation, cancellation))
                    {
                        _informationCancellation = null;
                        AnimationInfoOverlay.IsVisible = false;
                    }
                    cancellation.Dispose();
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Kan inte spela {Path.GetFileName(path)}: {exception.Message}");
            Show(DisplayMode.Time);
        }
    }

    private async Task PlayLibraryOffsetAsync(int offset, bool automatic = false)
    {
        if (_libraryRoot is null || _playableItems.Count == 0) return;
        if (!automatic)
        {
            _animationsRemainingInCycle = 0;
            _nextAnimationAtUtc = null;
        }
        if (_randomMode && offset > 0)
            _libraryPosition = Random.Shared.Next(_playableItems.Count);
        else
            _libraryPosition = (_libraryPosition + offset + _playableItems.Count) % _playableItems.Count;

        var item = _playableItems[_libraryPosition];
        await LoadAndPlayAsync(
            Path.Combine(_libraryRoot, item.RelativePath.Replace('/', Path.DirectorySeparatorChar)),
            item.RelativePath);
    }

    private void MoveFrame(int offset)
    {
        if (_playback is null) return;
        var now = DateTimeOffset.UtcNow;
        if (offset > 0) _playback.MoveNext(now); else _playback.MovePrevious(now);
        if (_isPaused) _playback.Pause(now);
        _displayMode = DisplayMode.Animation;
        Display.Frame = RenderPlaybackFrame(DateTimeOffset.Now);
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        var now = DateTimeOffset.UtcNow;
        if (_playback is not null)
        {
            if (_isPaused) _playback.Pause(now); else _playback.Resume(now);
        }
        UpdateTitle();
    }

    private void Show(DisplayMode mode)
    {
        CancelInformationDisplay();
        _displayMode = mode;
        _playback = null;
        _animationsRemainingInCycle = 0;
        _nextAnimationAtUtc = null;
        if (mode == DisplayMode.Time)
            _clockUntilUtc = DateTimeOffset.UtcNow.AddSeconds(_settings.ClockDisplaySeconds);
        UpdateClockOrDate();
        LogDisplayedMode(mode);
    }

    private void UpdateClockOrDate()
    {
        var now = DateTimeOffset.Now;
        _lastClockRender = now;
        Display.Frame = _displayMode == DisplayMode.Date ? CreateDateFrame(now) : CreateClockFrame(now);
    }

    private DmdFrame RenderPlaybackFrame(DateTimeOffset now)
    {
        var playback = _playback ?? throw new InvalidOperationException("No scene is currently playing.");
        var storyboard = playback.Storyboard;
        var clock = storyboard.ClockStyle == 1
            ? ClockFrameFactory.CreateCompactTime(now, storyboard.CustomX, storyboard.CustomY, _settings.ClockFormat == "12")
            : CreateClockFrame(now);
        return DmdFrameCompositor.Compose(playback.CurrentFrame, clock, playback.ClockAbove);
    }

    private DmdFrame CreateClockFrame(DateTimeOffset now)
    {
        var fallback = () => ClockFrameFactory.Create(now, _settings.ClockFormat == "12", _settings.ShowSeconds ?? true);
        var format = _settings.ClockFormat == "12"
            ? ((_settings.ShowSeconds ?? true) ? "hh:mm:ss tt" : "hh:mm tt")
            : ((_settings.ShowSeconds ?? true) ? "HH:mm:ss" : "HH:mm");
        return CreateOpenTypeFrame(now.ToString(format), _settings.ClockFontFile, fallback);
    }

    private DmdFrame CreateDateFrame(DateTimeOffset now)
    {
        var format = _settings.DateFormat ?? "yyyy-MM-dd";
        return CreateOpenTypeFrame(now.ToString(format), _settings.DateFontFile,
            () => ClockFrameFactory.CreateDate(now, format));
    }

    private static DmdFrame CreateOpenTypeFrame(string text, string? relativeFontFile, Func<DmdFrame> fallback)
    {
        var fontPath = ResolveFontPath(relativeFontFile);
        if (fontPath is null) return fallback();
        try { return OpenTypeDmdFrameFactory.Create(text, fontPath); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                          ArgumentException or InvalidOperationException or TypeInitializationException)
        {
            return fallback();
        }
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.FullScreen)
            WindowState = _windowStateBeforeFullscreen;
        else
        {
            _windowStateBeforeFullscreen = WindowState;
            WindowState = WindowState.FullScreen;
        }
    }

    private void StartLibraryWatcher()
    {
        _libraryWatcher?.Dispose();
        if (_libraryRoot is null) return;
        _libraryWatcher = new FileSystemWatcher(_libraryRoot)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _libraryWatcher.Created += LibraryChanged;
        _libraryWatcher.Changed += LibraryChanged;
        _libraryWatcher.Deleted += LibraryChanged;
        _libraryWatcher.Renamed += LibraryChanged;
        _libraryWatcher.Error += (_, _) => ScheduleRescan();
    }

    private void LibraryChanged(object sender, FileSystemEventArgs e)
    {
        if (string.Equals(Path.GetExtension(e.FullPath), ".scn", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(e.FullPath), SceneMetadataStore.DefaultFileName, StringComparison.OrdinalIgnoreCase))
            ScheduleRescan();
    }

    private void ScheduleRescan()
    {
        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _rescanCancellation, cancellation);
        previous?.Cancel();
        _ = DebouncedRescanAsync(cancellation.Token);
    }

    private async Task DebouncedRescanAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(750, cancellationToken);
            Dispatcher.UIThread.Post(() => _ = ScanLibraryAsync(startPlayback: false));
        }
        catch (OperationCanceledException) { }
    }

    private void SetStatus(string status)
    {
        _status = status;
        UpdateTitle();
    }

    private void UpdateTitle() => Title = "DMD Clock" +
        (_isPaused ? " — Pausad" : string.Empty) +
        (_status is null ? string.Empty : $" — {_status}");

    private static string SanitizeLogValue(string value) => value
        .Replace('\\', '/')
        .Replace('"', '\'')
        .Replace('\r', ' ')
        .Replace('\n', ' ');

    private void LogDisplayedMode(DisplayMode mode)
    {
        if (_startupBrandVisible) return;
        var type = mode == DisplayMode.Date ? "date" : "clock";
        LogDisplayChange(type, $"display.show type={type}");
    }

    private void LogDisplayedAnimation(string path, int frameCount, ResolvedSceneMetadata metadata)
    {
        var fullPath = Path.GetFullPath(path);
        var game = metadata.Game is null ? string.Empty : $" game=\"{SanitizeLogValue(metadata.Game)}\"";
        var title = metadata.Title is null ? string.Empty : $" title=\"{SanitizeLogValue(metadata.Title)}\"";
        var manufacturer = metadata.Manufacturer is null
            ? string.Empty
            : $" manufacturer=\"{SanitizeLogValue(metadata.Manufacturer)}\"";
        var year = metadata.Year is null ? string.Empty : $" year={metadata.Year}";
        LogDisplayChange($"animation:{fullPath}",
            $"display.show type=animation path=\"{SanitizeLogValue(fullPath)}\" " +
            $"file=\"{SanitizeLogValue(metadata.FileName)}\"{game}{title}{manufacturer}{year} frames={frameCount}");
    }

    private void LogDisplayedInformation(string path, ResolvedSceneMetadata metadata, string sequence)
    {
        var fullPath = Path.GetFullPath(path);
        var game = metadata.Game ?? "Okänt spel";
        LogDisplayChange($"information:{fullPath}",
            $"display.show type=information path=\"{SanitizeLogValue(fullPath)}\" " +
            $"file=\"{SanitizeLogValue(metadata.FileName)}\" game=\"{SanitizeLogValue(game)}\" " +
            $"sequence=\"{SanitizeLogValue(sequence)}\" durationMs={AnimationInformationDuration.TotalMilliseconds:F0}");
    }

    private ResolvedSceneMetadata ResolveSceneMetadata(string fullPath, string? relativePath)
    {
        if (relativePath is not null) return _sceneMetadata.Resolve(relativePath);
        if (_libraryRoot is not null)
        {
            var candidate = Path.GetRelativePath(_libraryRoot, fullPath);
            if (!candidate.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && candidate != "..")
                return _sceneMetadata.Resolve(candidate);
        }
        return SceneMetadataCatalog.Empty.Resolve(Path.GetFileName(fullPath));
    }

    private async Task LoadSceneMetadataAsync()
    {
        if (_libraryRoot is null) return;
        var path = Path.Combine(_libraryRoot, SceneMetadataStore.DefaultFileName);
        try
        {
            _sceneMetadata = await _metadataStore.LoadAsync(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                          System.Text.Json.JsonException or ArgumentException)
        {
            _sceneMetadata = SceneMetadataCatalog.Empty;
            await _log.WriteAsync(DateTimeOffset.UtcNow,
                $"metadata.load status=failed path=\"{SanitizeLogValue(path)}\" " +
                $"error=\"{SanitizeLogValue(exception.Message)}\"");
        }
    }

    private void LogDisplayChange(string key, string message)
    {
        if (string.Equals(_lastLoggedDisplay, key, StringComparison.Ordinal)) return;
        _lastLoggedDisplay = key;
        _ = _log.WriteAsync(DateTimeOffset.UtcNow, message);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _displayTimer.Stop();
        _startupBrandCancellation.Cancel();
        _startupBrandCancellation.Dispose();
        CancelInformationDisplay();
        _libraryWatcher?.Dispose();
        _rescanCancellation?.Cancel();
        _rescanCancellation?.Dispose();
        var endedUtc = DateTimeOffset.UtcNow;
        var reason = _exitRequestedByMenu ? "menu" : "window";
        _log.WriteAsync(endedUtc,
                $"app.exit graceful=true reason={reason} build=\"{SanitizeLogValue(_buildId)}\" " +
                $"startedUtc={_startedUtc:O} endUtc={endedUtc:O} uptimeMs={(endedUtc - _startedUtc).TotalMilliseconds:F0}")
            .GetAwaiter().GetResult();
    }

    private void LogStartup()
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        _log.WriteAsync(_startedUtc,
                $"app.start build=\"{SanitizeLogValue(_buildId)}\" version={assemblyVersion} " +
                $"pid={Environment.ProcessId} runtime=\"{SanitizeLogValue(RuntimeInformation.FrameworkDescription)}\" " +
                $"os=\"{SanitizeLogValue(RuntimeInformation.OSDescription)}\" startUtc={_startedUtc:O} " +
                $"basePath=\"{SanitizeLogValue(AppContext.BaseDirectory)}\"")
            .GetAwaiter().GetResult();
    }

    private static string GetBuildId() =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

    private void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        LogDisplayChange("brand:alien-tech",
            $"display.show type=brand name=\"Alien Tech\" durationMs={StartupBrandDuration.TotalMilliseconds:F0}");
        _ = HideStartupBrandAsync(_startupBrandCancellation.Token);
    }

    private async Task HideStartupBrandAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(StartupBrandDuration, cancellationToken);
            _startupBrandVisible = false;
            StartupBrandOverlay.IsVisible = false;
            if (_displayMode is DisplayMode.Time or DisplayMode.Date)
                LogDisplayedMode(_displayMode);
        }
        catch (OperationCanceledException) { }
    }

    private async Task InitializeDefaultLibraryAsync()
    {
        _settings = await _settingsStore.LoadAsync(_settingsPath);
        LocalizationManager.Load(_settings.Language ?? "en");
        StartupAnimationCountText.Text = L("startupLoading");
        ApplyMenuTranslations(MainContextMenu.Items);
        PopulateFontMenus();
        _randomMode = _settings.RandomPlayback;
        ApplySettingsToMenu();
        Show(DisplayMode.Time);
        _libraryRoot = ResolveDefaultScenesDirectory();
        await ScanLibraryAsync(startPlayback: false);
        StartLibraryWatcher();
    }

    private async Task BeginAutomaticCycleAsync()
    {
        try
        {
            _animationsRemainingInCycle = _settings.AnimationsPerCycle;
            await PlayNextAutomaticAnimationAsync();
        }
        finally
        {
            _automaticStartInProgress = false;
        }
    }

    private async Task PlayNextAutomaticAnimationAsync()
    {
        if (_animationsRemainingInCycle <= 0)
        {
            Show(DisplayMode.Time);
            return;
        }

        _animationsRemainingInCycle--;
        await PlayLibraryOffsetAsync(1, automatic: true);
    }

    private async Task ContinueAutomaticCycleAsync()
    {
        try
        {
            await PlayNextAutomaticAnimationAsync();
        }
        finally
        {
            _automaticStartInProgress = false;
        }
    }

    private void HandlePlaybackCompleted()
    {
        _playback = null;
        if (_animationsRemainingInCycle > 0)
        {
            if (_settings.AnimationGapSeconds == 0)
                _ = PlayNextAutomaticAnimationAsync();
            else
            {
                _displayMode = DisplayMode.Time;
                _nextAnimationAtUtc = DateTimeOffset.UtcNow.AddSeconds(_settings.AnimationGapSeconds);
                UpdateClockOrDate();
                LogDisplayedMode(DisplayMode.Time);
                SetStatus($"Nästa animation om {_settings.AnimationGapSeconds} sekunder");
            }
        }
        else
            Show(DisplayMode.Time);
    }

    private void ApplySettingsToMenu()
    {
        RandomMenuItem.Header = Check(_settings.RandomPlayback, L("random"));
        AutomaticCycleMenuItem.Header = Check(_settings.AutomaticCycle, L("automatic"));
        var preset = _settings.ColorPreset ?? DmdColorPreset.Orange;
        AppearanceOrangeMenuItem.Header = Check(preset == DmdColorPreset.Orange, L("orange"));
        AppearanceRedMenuItem.Header = Check(preset == DmdColorPreset.Red, L("red"));
        AppearancePlasmaMenuItem.Header = Check(preset == DmdColorPreset.Plasma, L("plasma"));
        AppearanceMonochromeMenuItem.Header = Check(preset == DmdColorPreset.Monochrome, L("monochrome"));
        var brightness = _settings.BrightnessPercent ?? 100;
        Brightness25MenuItem.Header = Check(brightness == 25, "25 %");
        Brightness50MenuItem.Header = Check(brightness == 50, "50 %");
        Brightness75MenuItem.Header = Check(brightness == 75, "75 %");
        Brightness100MenuItem.Header = Check(brightness == 100, "100 %");
        GlowMenuItem.Header = Check(_settings.GlowEnabled ?? true, L("glow"));
        ForegroundColorMenuItem.Header = $"{L("foregroundColor")}: {_settings.ForegroundColor ?? L("colorThemeValue")}";
        BackgroundColorMenuItem.Header = $"{L("backgroundColor")}: {_settings.BackgroundColor ?? "#000000"}";
        AnimationInfoMenuItem.Header = Check(_settings.ShowAnimationInfo ?? true, L("animationInfo"));
        EnglishLanguageMenuItem.Header = Check((_settings.Language ?? "en") == "en", L("english"));
        SwedishLanguageMenuItem.Header = Check(_settings.Language == "sv", L("swedish"));
        Clock24MenuItem.Header = Check(_settings.ClockFormat != "12", L("hour24"));
        Clock12MenuItem.Header = Check(_settings.ClockFormat == "12", L("hour12"));
        ShowSecondsMenuItem.Header = Check(_settings.ShowSeconds ?? true, L("showSeconds"));
        ShowTitleBarMenuItem.Header = Check(_settings.ShowTitleBar ?? true, L("showTitleBar"));
        WindowDecorations = (_settings.ShowTitleBar ?? true)
            ? Avalonia.Controls.WindowDecorations.Full
            : Avalonia.Controls.WindowDecorations.None;
        var dateFormat = _settings.DateFormat ?? "yyyy-MM-dd";
        DateIsoMenuItem.Header = Check(dateFormat == "yyyy-MM-dd", L("dateIso"));
        DateEuropeanMenuItem.Header = Check(dateFormat == "dd/MM/yyyy", L("dateEuropean"));
        DateUsMenuItem.Header = Check(dateFormat == "MM/dd/yyyy", L("dateUs"));
        DateDotsMenuItem.Header = Check(dateFormat == "dd.MM.yyyy", L("dateDots"));
        ApplyFontMenuChecks();
        ClockTime10MenuItem.Header = Check(_settings.ClockDisplaySeconds == 10, L("seconds10"));
        ClockTime30MenuItem.Header = Check(_settings.ClockDisplaySeconds == 30, L("seconds30"));
        ClockTime60MenuItem.Header = Check(_settings.ClockDisplaySeconds == 60, L("seconds60"));
        Animations1MenuItem.Header = Check(_settings.AnimationsPerCycle == 1, L("animation1"));
        Animations3MenuItem.Header = Check(_settings.AnimationsPerCycle == 3, L("animation3"));
        Animations5MenuItem.Header = Check(_settings.AnimationsPerCycle == 5, L("animation5"));
        AnimationGap0MenuItem.Header = Check(_settings.AnimationGapSeconds == 0, L("noPause"));
        AnimationGap5MenuItem.Header = Check(_settings.AnimationGapSeconds == 5, L("seconds5"));
        AnimationGap10MenuItem.Header = Check(_settings.AnimationGapSeconds == 10, L("seconds10"));
        AnimationGap30MenuItem.Header = Check(_settings.AnimationGapSeconds == 30, L("seconds30"));
        Display.SetAppearance(preset, brightness, _settings.GlowEnabled ?? true,
            _settings.ForegroundColor, _settings.BackgroundColor);
    }

    private static string Check(bool selected, string label) => selected ? $"✓ {label}" : label;
    private static string L(string key) => LocalizationManager.Get(key);

    private void PopulateFontMenus()
    {
        _clockFontItems.Clear();
        _dateFontItems.Clear();
        ClockFontMenuItem.Items.Clear();
        DateFontMenuItem.Items.Clear();
        AddFontMenuItem(ClockFontMenuItem, _clockFontItems, null, L("builtInFont"), SetClockFont);
        AddFontMenuItem(DateFontMenuItem, _dateFontItems, null, L("builtInFont"), SetDateFont);

        var fontsDirectory = Path.Combine(AppContext.BaseDirectory, "fonts");
        if (Directory.Exists(fontsDirectory))
        {
            var files = Directory.EnumerateFiles(fontsDirectory, "*", SearchOption.AllDirectories)
                .Where(IsSupportedFontFile)
                .Select(path => new
                {
                    Path = path,
                    Relative = Path.GetRelativePath(fontsDirectory, path).Replace('\\', '/')
                })
                .OrderBy(font => Path.GetFileNameWithoutExtension(font.Path), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(font => font.Relative, StringComparer.OrdinalIgnoreCase);
            foreach (var font in files)
            {
                var label = Path.GetFileNameWithoutExtension(font.Path);
                AddFontMenuItem(ClockFontMenuItem, _clockFontItems, font.Relative, label, SetClockFont);
                AddFontMenuItem(DateFontMenuItem, _dateFontItems, font.Relative, label, SetDateFont);
            }
        }
        ApplyFontMenuChecks();
    }

    private static void AddFontMenuItem(MenuItem parent, Dictionary<MenuItem, string?> items,
        string? relativePath, string label, Action<string?> selected)
    {
        var item = new MenuItem { Header = label, StaysOpenOnClick = true };
        item.Click += (_, _) => selected(relativePath);
        items[item] = relativePath;
        parent.Items.Add(item);
    }

    private void ApplyFontMenuChecks()
    {
        foreach (var (item, path) in _clockFontItems)
            item.Header = Check(string.Equals(path, _settings.ClockFontFile, StringComparison.OrdinalIgnoreCase),
                path is null ? L("builtInFont") : Path.GetFileNameWithoutExtension(path));
        foreach (var (item, path) in _dateFontItems)
            item.Header = Check(string.Equals(path, _settings.DateFontFile, StringComparison.OrdinalIgnoreCase),
                path is null ? L("builtInFont") : Path.GetFileNameWithoutExtension(path));
    }

    private static bool IsSupportedFontFile(string path) =>
        Path.GetExtension(path) is { } extension &&
        (extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
         extension.Equals(".otf", StringComparison.OrdinalIgnoreCase));

    private static string? ResolveFontPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fonts"));
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(candidate) || !IsSupportedFontFile(candidate)) return null;
        return candidate;
    }

    private static void ApplyMenuTranslations(IEnumerable<object?> items)
    {
        foreach (var menuItem in items.OfType<MenuItem>())
        {
            var key = menuItem.Tag as string;
            if (key is null && menuItem.Header is string header)
            {
                key = LocalizationManager.FindKey(header);
                if (key is not null) menuItem.Tag = key;
            }
            if (key is not null) menuItem.Header = L(key);
            ApplyMenuTranslations(menuItem.Items);
        }
    }

    private void SetLanguage(string language)
    {
        _settings = (_settings with { Language = language }).Normalize();
        LocalizationManager.Load(_settings.Language ?? "en");
        ApplyMenuTranslations(MainContextMenu.Items);
        PopulateFontMenus();
        ApplySettingsToMenu();
        SaveSettings();
    }

    private void SetClockFormat(string format)
    {
        _settings = (_settings with { ClockFormat = format }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        if (_displayMode == DisplayMode.Time) UpdateClockOrDate();
    }

    private void ToggleShowSeconds()
    {
        _settings = (_settings with { ShowSeconds = !(_settings.ShowSeconds ?? true) }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        if (_displayMode == DisplayMode.Time) UpdateClockOrDate();
    }

    private void ToggleTitleBar()
    {
        _settings = (_settings with { ShowTitleBar = !(_settings.ShowTitleBar ?? true) }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
    }

    private void SetDateFormat(string format)
    {
        _settings = (_settings with { DateFormat = format }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        if (_displayMode == DisplayMode.Date) UpdateClockOrDate();
    }

    private void SetClockFont(string? relativePath)
    {
        _settings = (_settings with { ClockFontFile = relativePath }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        if (_displayMode == DisplayMode.Time) UpdateClockOrDate();
    }

    private void SetDateFont(string? relativePath)
    {
        _settings = (_settings with { DateFontFile = relativePath }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        if (_displayMode == DisplayMode.Date) UpdateClockOrDate();
    }

    private void SaveSettings() => _ = _settingsStore.SaveAtomicAsync(_settings, _settingsPath);

    private void SetClockDisplaySeconds(int seconds)
    {
        _settings = (_settings with { ClockDisplaySeconds = seconds }).Normalize();
        ApplySettingsToMenu();
        if (_displayMode == DisplayMode.Time)
            _clockUntilUtc = DateTimeOffset.UtcNow.AddSeconds(_settings.ClockDisplaySeconds);
        SaveSettings();
        SetStatus($"Klocktid: {_settings.ClockDisplaySeconds} sekunder");
    }

    private void SetAnimationsPerCycle(int count)
    {
        _settings = (_settings with { AnimationsPerCycle = count }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        SetStatus($"Animationer per cykel: {_settings.AnimationsPerCycle}");
    }

    private void SetAnimationGapSeconds(int seconds)
    {
        _settings = (_settings with { AnimationGapSeconds = seconds }).Normalize();
        ApplySettingsToMenu();
        if (_nextAnimationAtUtc is not null)
            _nextAnimationAtUtc = DateTimeOffset.UtcNow.AddSeconds(_settings.AnimationGapSeconds);
        SaveSettings();
        SetStatus(_settings.AnimationGapSeconds == 0
            ? "Ingen paus mellan animationer"
            : $"Tid mellan animationer: {_settings.AnimationGapSeconds} sekunder");
    }

    private void SetColorPreset(DmdColorPreset preset)
    {
        _settings = (_settings with { ColorPreset = preset, ForegroundColor = null }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        SetStatus($"Färgtema: {PresetName(preset)}");
    }

    private async Task PickColorAsync(bool foreground)
    {
        var initial = foreground
            ? ParseDisplayColor(_settings.ForegroundColor, PresetColor(_settings.ColorPreset ?? DmdColorPreset.Orange))
            : ParseDisplayColor(_settings.BackgroundColor, Colors.Black);
        var dialog = new ColorPickerWindow(
            foreground ? L("foregroundColor") : L("backgroundColor"), initial, L("ok"), L("cancel"));
        var selected = await dialog.ShowDialog<Color?>(this);
        if (selected is not { } color) return;
        var value = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        _settings = foreground
            ? (_settings with { ForegroundColor = value }).Normalize()
            : (_settings with { BackgroundColor = value }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        SetStatus($"{(foreground ? L("foregroundColor") : L("backgroundColor"))}: {value}");
    }

    private static Color ParseDisplayColor(string? value, Color fallback)
    {
        try { return string.IsNullOrWhiteSpace(value) ? fallback : Color.Parse(value); }
        catch (FormatException) { return fallback; }
    }

    private static Color PresetColor(DmdColorPreset preset) => preset switch
    {
        DmdColorPreset.Red => Color.FromRgb(255, 32, 16),
        DmdColorPreset.Plasma => Color.FromRgb(120, 100, 255),
        DmdColorPreset.Monochrome => Color.FromRgb(235, 235, 235),
        _ => Color.FromRgb(255, 112, 14)
    };

    private void SetBrightness(int percent)
    {
        _settings = (_settings with { BrightnessPercent = percent }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        SetStatus($"Ljusstyrka: {_settings.BrightnessPercent} %");
    }

    private void ToggleAnimationInformation()
    {
        var enabled = !(_settings.ShowAnimationInfo ?? true);
        _settings = (_settings with { ShowAnimationInfo = enabled }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        if (!enabled) CancelInformationDisplay();
        SetStatus(enabled ? "Animationsinformation: på" : "Animationsinformation: av");
    }

    private void CancelInformationDisplay()
    {
        var cancellation = _informationCancellation;
        _informationCancellation = null;
        AnimationInfoOverlay.IsVisible = false;
        cancellation?.Cancel();
    }

    private static string PresetName(DmdColorPreset preset) => preset switch
    {
        DmdColorPreset.Red => "röd",
        DmdColorPreset.Plasma => "plasma",
        DmdColorPreset.Monochrome => "monokrom",
        _ => "klassisk orange"
    };

    private static string ResolveDefaultScenesDirectory()
    {
        var installedDirectory = Path.Combine(AppContext.BaseDirectory, "scenes");
        if (Directory.Exists(installedDirectory) &&
            Directory.EnumerateFiles(installedDirectory, "*.scn", SearchOption.AllDirectories).Any())
            return installedDirectory;

        var workingDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "scenes"));
        if (Directory.Exists(workingDirectory)) return workingDirectory;

        Directory.CreateDirectory(installedDirectory);
        return installedDirectory;
    }

    private async void OpenScene_Click(object? sender, RoutedEventArgs e) => await OpenSceneAsync();
    private async void ChooseFolder_Click(object? sender, RoutedEventArgs e) => await ChooseFolderAsync();
    private async void Rescan_Click(object? sender, RoutedEventArgs e) => await ScanLibraryAsync(startPlayback: false);
    private void MainContextMenu_Opened(object? sender, RoutedEventArgs e) => PopulateFontMenus();
    private void PlayPause_Click(object? sender, RoutedEventArgs e) => TogglePause();
    private void NextFrame_Click(object? sender, RoutedEventArgs e) => MoveFrame(1);
    private void PreviousFrame_Click(object? sender, RoutedEventArgs e) => MoveFrame(-1);
    private async void NextAnimation_Click(object? sender, RoutedEventArgs e) => await PlayLibraryOffsetAsync(1);
    private async void PreviousAnimation_Click(object? sender, RoutedEventArgs e) => await PlayLibraryOffsetAsync(-1);
    private void RandomMode_Click(object? sender, RoutedEventArgs e)
    {
        _randomMode = !_randomMode;
        _settings = _settings with { RandomPlayback = _randomMode };
        ApplySettingsToMenu();
        SaveSettings();
    }
    private void AutomaticCycle_Click(object? sender, RoutedEventArgs e)
    {
        _settings = _settings with { AutomaticCycle = !_settings.AutomaticCycle };
        ApplySettingsToMenu();
        if (_settings.AutomaticCycle && _displayMode == DisplayMode.Time)
            _clockUntilUtc = DateTimeOffset.UtcNow.AddSeconds(_settings.ClockDisplaySeconds);
        SaveSettings();
    }
    private void ClockTime10_Click(object? sender, RoutedEventArgs e) => SetClockDisplaySeconds(10);
    private void ClockTime30_Click(object? sender, RoutedEventArgs e) => SetClockDisplaySeconds(30);
    private void ClockTime60_Click(object? sender, RoutedEventArgs e) => SetClockDisplaySeconds(60);
    private void Animations1_Click(object? sender, RoutedEventArgs e) => SetAnimationsPerCycle(1);
    private void Animations3_Click(object? sender, RoutedEventArgs e) => SetAnimationsPerCycle(3);
    private void Animations5_Click(object? sender, RoutedEventArgs e) => SetAnimationsPerCycle(5);
    private void AnimationGap0_Click(object? sender, RoutedEventArgs e) => SetAnimationGapSeconds(0);
    private void AnimationGap5_Click(object? sender, RoutedEventArgs e) => SetAnimationGapSeconds(5);
    private void AnimationGap10_Click(object? sender, RoutedEventArgs e) => SetAnimationGapSeconds(10);
    private void AnimationGap30_Click(object? sender, RoutedEventArgs e) => SetAnimationGapSeconds(30);
    private void AppearanceOrange_Click(object? sender, RoutedEventArgs e) => SetColorPreset(DmdColorPreset.Orange);
    private void AppearanceRed_Click(object? sender, RoutedEventArgs e) => SetColorPreset(DmdColorPreset.Red);
    private void AppearancePlasma_Click(object? sender, RoutedEventArgs e) => SetColorPreset(DmdColorPreset.Plasma);
    private void AppearanceMonochrome_Click(object? sender, RoutedEventArgs e) => SetColorPreset(DmdColorPreset.Monochrome);
    private void Brightness25_Click(object? sender, RoutedEventArgs e) => SetBrightness(25);
    private void Brightness50_Click(object? sender, RoutedEventArgs e) => SetBrightness(50);
    private void Brightness75_Click(object? sender, RoutedEventArgs e) => SetBrightness(75);
    private void Brightness100_Click(object? sender, RoutedEventArgs e) => SetBrightness(100);
    private void Glow_Click(object? sender, RoutedEventArgs e)
    {
        _settings = (_settings with { GlowEnabled = !(_settings.GlowEnabled ?? true) }).Normalize();
        ApplySettingsToMenu();
        SaveSettings();
        SetStatus((_settings.GlowEnabled ?? true) ? "Glöd: på" : "Glöd: av");
    }
    private async void ForegroundColor_Click(object? sender, RoutedEventArgs e) => await PickColorAsync(foreground: true);
    private async void BackgroundColor_Click(object? sender, RoutedEventArgs e) => await PickColorAsync(foreground: false);
    private void AnimationInfo_Click(object? sender, RoutedEventArgs e) => ToggleAnimationInformation();
    private void ShowTime_Click(object? sender, RoutedEventArgs e) => Show(DisplayMode.Time);
    private void ShowDate_Click(object? sender, RoutedEventArgs e) => Show(DisplayMode.Date);
    private void Fullscreen_Click(object? sender, RoutedEventArgs e) => ToggleFullscreen();
    private void HelpGitHub_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(HelpGitHubUrl) { UseShellExecute = true });
            _ = _log.WriteAsync(DateTimeOffset.UtcNow, $"help.open url=\"{HelpGitHubUrl}\"");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            SetStatus($"Kan inte öppna GitHub: {exception.Message}");
        }
    }
    private void EnglishLanguage_Click(object? sender, RoutedEventArgs e) => SetLanguage("en");
    private void SwedishLanguage_Click(object? sender, RoutedEventArgs e) => SetLanguage("sv");
    private void Clock24_Click(object? sender, RoutedEventArgs e) => SetClockFormat("24");
    private void Clock12_Click(object? sender, RoutedEventArgs e) => SetClockFormat("12");
    private void ShowSeconds_Click(object? sender, RoutedEventArgs e) => ToggleShowSeconds();
    private void ShowTitleBar_Click(object? sender, RoutedEventArgs e) => ToggleTitleBar();
    private void DateIso_Click(object? sender, RoutedEventArgs e) => SetDateFormat("yyyy-MM-dd");
    private void DateEuropean_Click(object? sender, RoutedEventArgs e) => SetDateFormat("dd/MM/yyyy");
    private void DateUs_Click(object? sender, RoutedEventArgs e) => SetDateFormat("MM/dd/yyyy");
    private void DateDots_Click(object? sender, RoutedEventArgs e) => SetDateFormat("dd.MM.yyyy");
    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        _exitRequestedByMenu = true;
        Close();
    }

    private void Display_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!(_settings.ShowTitleBar ?? true) && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private enum DisplayMode { Time, Date, Animation }
}
