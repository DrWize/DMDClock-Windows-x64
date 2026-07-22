namespace DmdClock.Core.Colorization;

public enum FrameMatchStatus
{
    None,
    Unique,
    Ambiguous
}

public sealed class FrameMatchResult
{
    internal FrameMatchResult(IReadOnlyList<StaticColorizationRule> matches)
    {
        Matches = Array.AsReadOnly(matches.ToArray());
        Status = Matches.Count switch
        {
            0 => FrameMatchStatus.None,
            1 => FrameMatchStatus.Unique,
            _ => FrameMatchStatus.Ambiguous
        };
    }

    public FrameMatchStatus Status { get; }
    public IReadOnlyList<StaticColorizationRule> Matches { get; }
    public StaticColorizationRule? UniqueRule => Status == FrameMatchStatus.Unique ? Matches[0] : null;
    public IReadOnlyList<string> MatchingRuleIds => Matches.Select(static rule => rule.Id).ToArray();
}
