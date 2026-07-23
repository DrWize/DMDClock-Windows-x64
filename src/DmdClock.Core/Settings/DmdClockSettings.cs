using System.Globalization;

namespace DmdClock.Core.Settings;

public enum DmdColorPreset
{
    Orange,
    Red,
    Plasma,
    Monochrome,
    NeonSunset,
    CyberOcean,
    ToxicArcade,
    Vaporwave,
    Aurora,
    C64BlueRound,
    C64RedRound,
    C64Earthtone,
    C64Metal,
    C64InterlacedBlue,
    C64ExtrudedCyan,
    C64Rainbow
}

public sealed record DmdClockSettings(
    int SchemaVersion,
    bool AutomaticCycle,
    bool RandomPlayback,
    int ClockDisplaySeconds,
    int AnimationsPerCycle,
    int AnimationGapSeconds,
    DmdColorPreset? ColorPreset,
    int? BrightnessPercent,
    bool? GlowEnabled,
    bool? ShowAnimationInfo,
    string? Language,
    string? ClockFormat,
    string? DateFormat,
    bool? ShowSeconds,
    bool? ShowTitleBar,
    string? ClockFontFile,
    string? DateFontFile,
    string? ForegroundColor,
    string? BackgroundColor,
    int? WindowScalePercent,
    int? FullscreenZoomPercent)
{
    public const int CurrentSchemaVersion = 1;

    public static DmdClockSettings Default { get; } = new(
        CurrentSchemaVersion, true, false, 30, 1, 0, DmdColorPreset.Orange, 100, true, true, "en", "24", "yyyy-MM-dd", true, true, null, null, null, "#000000", 100, 100);

    public DmdClockSettings Normalize() => this with
    {
        SchemaVersion = CurrentSchemaVersion,
        ClockDisplaySeconds = Math.Clamp(ClockDisplaySeconds, 5, 3600),
        AnimationsPerCycle = Math.Clamp(AnimationsPerCycle, 1, 20),
        AnimationGapSeconds = Math.Clamp(AnimationGapSeconds, 0, 3600),
        ColorPreset = ColorPreset is { } preset && Enum.IsDefined(preset) ? preset : DmdColorPreset.Orange,
        BrightnessPercent = Math.Clamp(BrightnessPercent ?? 100, 25, 100),
        GlowEnabled = GlowEnabled ?? true,
        ShowAnimationInfo = ShowAnimationInfo ?? true,
        Language = Language is "sv" ? "sv" : "en",
        ClockFormat = ClockFormat is "12" ? "12" : "24",
        DateFormat = DateFormat is "dd/MM/yyyy" or "MM/dd/yyyy" or "dd.MM.yyyy" ? DateFormat : "yyyy-MM-dd",
        ShowSeconds = ShowSeconds ?? true,
        ShowTitleBar = ShowTitleBar ?? true,
        ClockFontFile = NormalizeFontFile(ClockFontFile),
        DateFontFile = NormalizeFontFile(DateFontFile),
        ForegroundColor = NormalizeColor(ForegroundColor),
        BackgroundColor = NormalizeColor(BackgroundColor) ?? "#000000",
        WindowScalePercent = NormalizeScale(WindowScalePercent),
        FullscreenZoomPercent = NormalizeScale(FullscreenZoomPercent)
    };

    private static string? NormalizeFontFile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().Replace('\\', '/').TrimStart('/');
        return normalized.Split('/').Any(part => part is ".." or "") ? null : normalized;
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length != 7 || normalized[0] != '#' ||
            !uint.TryParse(normalized.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            return null;
        return normalized.ToUpperInvariant();
    }

    private static int NormalizeScale(int? value)
    {
        var clamped = Math.Clamp(value ?? 100, 5, 5000);
        return (int)Math.Round(clamped / 5d) * 5;
    }
}
