using DmdClock.Core.Colorization;

namespace DmdClock.Core.Tests.Colorization;

public sealed class StaticFrameColorizerTests
{
    [Fact]
    public void Colorize_IgnoresDynamicPixelsAndReturnsUniqueColorFrame()
    {
        var source = Frame([0, 1, 2, 3]);
        var incoming = Frame([0, 3, 2, 3]);
        var mask = new ComparisonMask(4, 1, [0, 1, 0, 0]);
        var color = ColorFrame([0, 1, 1, 0]);
        var rule = new StaticColorizationRule("score-screen", source, mask, color);

        var result = StaticFrameColorizer.Colorize(incoming, [rule]);

        Assert.True(result.IsColorized);
        Assert.False(result.UsedMonochromeFallback);
        Assert.Same(color, result.ColorizedFrame);
        Assert.Equal(FrameMatchStatus.Unique, result.Match.Status);
        Assert.Equal(["score-screen"], result.Match.MatchingRuleIds);
    }

    [Fact]
    public void Colorize_WhenNoRuleMatches_ReturnsOriginalMonochromeFrame()
    {
        var incoming = Frame([3, 3, 3, 3]);
        var rule = Rule("different", [0, 1, 2, 3]);

        var result = StaticFrameColorizer.Colorize(incoming, [rule]);

        Assert.True(result.UsedMonochromeFallback);
        Assert.Null(result.ColorizedFrame);
        Assert.Same(incoming, result.OriginalFrame);
        Assert.Equal(FrameMatchStatus.None, result.Match.Status);
    }

    [Fact]
    public void Colorize_WhenSeveralRulesMatch_ReportsAmbiguityAndFallsBack()
    {
        var incoming = Frame([0, 1, 2, 3]);

        var result = StaticFrameColorizer.Colorize(incoming,
            [Rule("first", [0, 1, 2, 3]), Rule("second", [0, 1, 2, 3])]);

        Assert.True(result.UsedMonochromeFallback);
        Assert.Equal(FrameMatchStatus.Ambiguous, result.Match.Status);
        Assert.Equal(["first", "second"], result.Match.MatchingRuleIds);
    }

    private static StaticColorizationRule Rule(string id, byte[] intensities) =>
        new(id, Frame(intensities), ComparisonMask.CompareAll(4, 1), ColorFrame([0, 0, 0, 0]));

    private static DmdFrame Frame(byte[] intensities) => new(4, 1, intensities);

    private static Palette6Frame ColorFrame(byte[] indices) => new(4, 1, indices,
        [new Rgb24(0, 0, 0), new Rgb24(255, 0, 0)]);
}
