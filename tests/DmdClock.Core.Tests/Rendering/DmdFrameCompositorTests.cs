using DmdClock.Core.Rendering;

namespace DmdClock.Core.Tests.Rendering;

public sealed class DmdFrameCompositorTests
{
    [Fact]
    public void Compose_UsesMaskAsTransparencyWithClockBehind()
    {
        var animationPixels = new byte[128 * 32];
        var animationMask = new byte[128 * 32];
        animationPixels[0] = 5;
        animationPixels[1] = 7;
        animationMask[0] = 1;
        var clockPixels = new byte[128 * 32];
        var clockMask = Enumerable.Repeat((byte)1, 128 * 32).ToArray();
        clockPixels[0] = 15;
        clockPixels[1] = 15;
        clockMask[0] = 0;
        clockMask[1] = 0;

        var result = DmdFrameCompositor.Compose(
            new DmdFrame(128, 32, animationPixels, animationMask),
            new DmdFrame(128, 32, clockPixels, clockMask),
            clockAbove: false);

        Assert.Equal(15, result.GetIntensity(0, 0));
        Assert.Equal(7, result.GetIntensity(1, 0));
    }

    [Fact]
    public void Compose_PutsOpaqueClockDotsAboveAnimation()
    {
        var animationPixels = Enumerable.Repeat((byte)6, 128 * 32).ToArray();
        var clockPixels = new byte[128 * 32];
        var clockMask = Enumerable.Repeat((byte)1, 128 * 32).ToArray();
        clockPixels[0] = 15;
        clockMask[0] = 0;

        var result = DmdFrameCompositor.Compose(
            new DmdFrame(128, 32, animationPixels),
            new DmdFrame(128, 32, clockPixels, clockMask),
            clockAbove: true);

        Assert.Equal(15, result.GetIntensity(0, 0));
        Assert.Equal(6, result.GetIntensity(1, 0));
    }
}

