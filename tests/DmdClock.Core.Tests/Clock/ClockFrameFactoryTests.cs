using DmdClock.Core.Clock;

namespace DmdClock.Core.Tests.Clock;

public sealed class ClockFrameFactoryTests
{
    [Fact]
    public void Create_ProducesValidDmdFrame()
    {
        var frame = ClockFrameFactory.Create(new DateTimeOffset(2026, 7, 22, 23, 45, 59, TimeSpan.Zero));

        Assert.Equal(128, frame.Width);
        Assert.Equal(32, frame.Height);
        Assert.Equal(128 * 32, frame.Intensities.Length);
        Assert.Contains((byte)15, frame.Intensities.ToArray());
        Assert.All(frame.Intensities.ToArray(), static value => Assert.InRange(value, (byte)0, (byte)15));
    }

    [Fact]
    public void CreateDate_ProducesValidDmdFrame()
    {
        var frame = ClockFrameFactory.CreateDate(new DateTimeOffset(2026, 7, 22, 23, 45, 59, TimeSpan.Zero));

        Assert.Equal(128, frame.Width);
        Assert.Equal(32, frame.Height);
        Assert.Contains((byte)15, frame.Intensities.ToArray());
    }

    [Theory]
    [InlineData("yyyy-MM-dd")]
    [InlineData("dd/MM/yyyy")]
    [InlineData("MM/dd/yyyy")]
    [InlineData("dd.MM.yyyy")]
    public void CreateDate_SupportsCommonFormats(string format)
    {
        var frame = ClockFrameFactory.CreateDate(new DateTimeOffset(2026, 7, 23, 13, 5, 9, TimeSpan.Zero), format);
        Assert.Contains((byte)15, frame.Intensities.ToArray());
    }

    [Fact]
    public void Create_SupportsTwelveHourClock()
    {
        var frame = ClockFrameFactory.Create(new DateTimeOffset(2026, 7, 23, 13, 5, 9, TimeSpan.Zero), twelveHour: true);
        Assert.Contains((byte)15, frame.Intensities.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_CanHideSecondsInBothClockFormats(bool twelveHour)
    {
        var frame = ClockFrameFactory.Create(
            new DateTimeOffset(2026, 7, 23, 13, 5, 9, TimeSpan.Zero),
            twelveHour,
            showSeconds: false);

        Assert.Equal(128, frame.Width);
        Assert.Equal(32, frame.Height);
        Assert.Contains((byte)15, frame.Intensities.ToArray());
    }

    [Fact]
    public void CreateInformation_HandlesLongNamesAndSwedishCharacters()
    {
        var frame = ClockFrameFactory.CreateInformation(
            "Indiana Jones: The Pinball Adventure",
            "Återkomst från templet – sekvens 123");

        Assert.Equal(128, frame.Width);
        Assert.Equal(32, frame.Height);
        Assert.Equal(128 * 32, frame.Intensities.Length);
        Assert.Contains((byte)15, frame.Intensities.ToArray());
    }

    [Fact]
    public void CreateInformation_UsesFallbackTextForMissingMetadata()
    {
        var frame = ClockFrameFactory.CreateInformation(null, " ");

        Assert.Contains((byte)15, frame.Intensities.ToArray());
    }
}
