using System.Text;
using DmdClock.Core.Colorization;

namespace DmdClock.Core.Tests.Colorization;

public sealed class SyntheticFrameDumpReaderTests
{
    [Fact]
    public void Read_ParsesRedistributableTimedFrames()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Colorization", "basic-v1.dmdump");

        var dump = SyntheticFrameDumpReader.Read(path);

        Assert.Equal(1, dump.Version);
        Assert.Equal(2, dump.SourceBitsPerPixel);
        Assert.Equal(2, dump.Frames.Count);
        Assert.Equal(TimeSpan.Zero, dump.Frames[0].Timestamp);
        Assert.Equal(TimeSpan.FromMilliseconds(40), dump.Frames[1].Timestamp);
        Assert.Equal(4, dump.Frames[0].Frame.Width);
        Assert.Equal(2, dump.Frames[0].Frame.Height);
        Assert.Equal((byte)2, dump.Frames[0].Frame.GetIntensity(1, 1));
        Assert.Equal((byte)0, dump.Frames[1].Frame.GetIntensity(1, 1));
    }

    [Fact]
    public void Read_RejectsPixelOutsideDeclaredBitDepth()
    {
        const string content = """
            DMD-DUMP 1 width=2 height=1 bpp=2
            FRAME timestampMs=0
            04
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var exception = Assert.Throws<FrameDumpFormatException>(() => SyntheticFrameDumpReader.Read(stream));

        Assert.Contains("outside the 2-bit source range", exception.Message);
    }
}
