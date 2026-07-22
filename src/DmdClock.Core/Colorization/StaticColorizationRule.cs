namespace DmdClock.Core.Colorization;

public sealed class StaticColorizationRule
{
    public StaticColorizationRule(
        string id,
        DmdFrame sourcePattern,
        ComparisonMask comparisonMask,
        Palette6Frame colorizedFrame)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(sourcePattern);
        ArgumentNullException.ThrowIfNull(comparisonMask);
        ArgumentNullException.ThrowIfNull(colorizedFrame);
        if (sourcePattern.Width != comparisonMask.Width || sourcePattern.Height != comparisonMask.Height ||
            sourcePattern.Width != colorizedFrame.Width || sourcePattern.Height != colorizedFrame.Height)
            throw new ArgumentException("Pattern, comparison mask and colorized frame must have identical dimensions.");

        Id = id;
        SourcePattern = sourcePattern;
        ComparisonMask = comparisonMask;
        ColorizedFrame = colorizedFrame;
    }

    public string Id { get; }
    public DmdFrame SourcePattern { get; }
    public ComparisonMask ComparisonMask { get; }
    public Palette6Frame ColorizedFrame { get; }
}
