using DmdClock.Core.Scn;

namespace DmdClock.Core.Tests.Scn;

public sealed class ScnReaderTests
{
    [Fact]
    public void Read_DecodesStoryboardPixelsAndMask()
    {
        using var stream = TestScnFile.Create(includeMask: true);

        var scene = ScnReader.Read(stream);

        Assert.Equal((ushort)1, scene.Version);
        var storyboard = Assert.Single(scene.Storyboards);
        Assert.Equal((ushort)125, storyboard.FrameDelayMs);
        Assert.True(storyboard.ClockAboveFrames);
        Assert.Equal((byte)7, storyboard.CustomX);

        var frame = Assert.Single(scene.Frames);
        Assert.Equal(1, frame.GetIntensity(0, 0));
        Assert.Equal(10, frame.GetIntensity(1, 0));
        Assert.True(frame.IsMasked(0, 0));
        Assert.False(frame.IsMasked(1, 0));
        Assert.True(frame.IsMasked(7, 0));
    }

    [Fact]
    public void Read_RejectsTruncatedData()
    {
        using var valid = TestScnFile.Create(includeMask: false);
        using var truncated = new MemoryStream(valid.ToArray()[..^1]);

        var exception = Assert.Throws<ScnFormatException>(() => ScnReader.Read(truncated));

        Assert.Contains("ended", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_RejectsUnknownVersion()
    {
        using var stream = TestScnFile.Create(includeMask: false);
        stream.GetBuffer()[0] = 2;

        var exception = Assert.Throws<ScnFormatException>(() => ScnReader.Read(stream));

        Assert.Contains("version 2", exception.Message);
    }

}
