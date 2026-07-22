using DmdClock.App.Rendering;
using DmdClock.Core.Scn;

namespace DmdClock.Core.Tests.Rendering;

public sealed class OpenTypeDmdFrameFactoryTests
{
    [Fact]
    public void Create_RasterizesBundledTrueTypeFontToFourBitFrame()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "fonts", "Inter", "InterVariable.ttf");

        var frame = OpenTypeDmdFrameFactory.Create("23:59", fontPath);

        Assert.Equal(ScnReader.DisplayWidth, frame.Width);
        Assert.Equal(ScnReader.DisplayHeight, frame.Height);
        Assert.Contains(frame.Intensities.ToArray(), intensity => intensity > 0);
        Assert.All(frame.Intensities.ToArray(), intensity => Assert.InRange(intensity, (byte)0, (byte)15));
    }
}
