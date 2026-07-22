namespace DmdClock.Core.Colorization;

public static class StaticFrameMatcher
{
    public static FrameMatchResult Match(DmdFrame incomingFrame, IReadOnlyList<StaticColorizationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(incomingFrame);
        ArgumentNullException.ThrowIfNull(rules);

        var matches = new List<StaticColorizationRule>();
        foreach (var rule in rules)
        {
            ArgumentNullException.ThrowIfNull(rule);
            if (IsMatch(incomingFrame, rule)) matches.Add(rule);
        }
        return new FrameMatchResult(matches);
    }

    private static bool IsMatch(DmdFrame incomingFrame, StaticColorizationRule rule)
    {
        var pattern = rule.SourcePattern;
        if (incomingFrame.Width != pattern.Width || incomingFrame.Height != pattern.Height) return false;

        var incoming = incomingFrame.Intensities.Span;
        var expected = pattern.Intensities.Span;
        var ignored = rule.ComparisonMask.IgnoredPixels.Span;
        for (var offset = 0; offset < incoming.Length; offset++)
        {
            if (ignored[offset] == 0 && incoming[offset] != expected[offset]) return false;
        }
        return true;
    }
}
