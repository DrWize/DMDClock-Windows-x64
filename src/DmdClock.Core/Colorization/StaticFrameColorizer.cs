namespace DmdClock.Core.Colorization;

public sealed record StaticColorizationResult(
    DmdFrame OriginalFrame,
    Palette6Frame? ColorizedFrame,
    FrameMatchResult Match)
{
    public bool IsColorized => ColorizedFrame is not null;
    public bool UsedMonochromeFallback => ColorizedFrame is null;
}

public static class StaticFrameColorizer
{
    public static StaticColorizationResult Colorize(
        DmdFrame incomingFrame,
        IReadOnlyList<StaticColorizationRule> rules)
    {
        var match = StaticFrameMatcher.Match(incomingFrame, rules);
        return new StaticColorizationResult(incomingFrame, match.UniqueRule?.ColorizedFrame, match);
    }
}
