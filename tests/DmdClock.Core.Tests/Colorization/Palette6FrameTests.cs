using DmdClock.Core.Colorization;

namespace DmdClock.Core.Tests.Colorization;

public sealed class Palette6FrameTests
{
    [Fact]
    public void ToRgb24_ExpandsPaletteIndicesAndExposesStride()
    {
        var frame = new Palette6Frame(2, 1, [1, 0],
            [new Rgb24(1, 2, 3), new Rgb24(250, 100, 50)]);

        var rgb = frame.ToRgb24();

        Assert.Equal(6, rgb.Stride);
        Assert.Equal(new Rgb24(250, 100, 50), rgb.GetPixel(0, 0));
        Assert.Equal(new Rgb24(1, 2, 3), rgb.GetPixel(1, 0));
        Assert.Equal(new byte[] { 250, 100, 50, 1, 2, 3 }, rgb.Pixels.ToArray());
    }

    [Fact]
    public void Constructor_RejectsMoreThan64Colors()
    {
        var palette = Enumerable.Range(0, 65).Select(static value => new Rgb24((byte)value, 0, 0)).ToArray();

        Assert.Throws<ArgumentException>(() => new Palette6Frame(1, 1, [0], palette));
    }
}
