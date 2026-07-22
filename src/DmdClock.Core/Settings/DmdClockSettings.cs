namespace DmdClock.Core.Settings;

public enum DmdColorPreset
{
    Orange,
    Red,
    Plasma,
    Monochrome
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
    string? DateFormat)
{
    public const int CurrentSchemaVersion = 1;

    public static DmdClockSettings Default { get; } = new(
        CurrentSchemaVersion, true, false, 30, 1, 0, DmdColorPreset.Orange, 100, true, true, "en", "24", "yyyy-MM-dd");

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
        DateFormat = DateFormat is "dd/MM/yyyy" or "MM/dd/yyyy" or "dd.MM.yyyy" ? DateFormat : "yyyy-MM-dd"
    };
}
