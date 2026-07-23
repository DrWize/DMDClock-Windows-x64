using DmdClock.App.Rendering;
using DmdClock.Core.Scn;

namespace DmdClock.Core.Tests.Rendering;

public sealed class EmbeddedDotClkFontsTests
{
    public static TheoryData<string> Fonts
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var id in EmbeddedDotClkFonts.Ids) data.Add(id);
            return data;
        }
    }

    public static TheoryData<string, string> DateSamples
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var id in EmbeddedDotClkFonts.Ids)
            foreach (var text in new[] { "2026-07-23", "23/07/2026", "07/23/2026", "23.07.2026" })
                data.Add(id, text);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Fonts))]
    public void Create_RendersTwentyFourAndTwelveHourTime(string id)
    {
        AssertUsableFrame(EmbeddedDotClkFonts.Create("23:59:58", id));
        AssertUsableFrame(EmbeddedDotClkFonts.Create("11:59:58 PM", id));
    }

    [Theory]
    [MemberData(nameof(DateSamples))]
    public void Create_RendersEveryDateFormat(string id, string text)
    {
        AssertUsableFrame(EmbeddedDotClkFonts.Create(text, id));
    }

    private static void AssertUsableFrame(DmdClock.Core.DmdFrame frame)
    {
        Assert.Equal(ScnReader.DisplayWidth, frame.Width);
        Assert.Equal(ScnReader.DisplayHeight, frame.Height);
        Assert.Contains(frame.Intensities.ToArray(), intensity => intensity > 0);
        Assert.All(frame.Intensities.ToArray(), intensity => Assert.InRange(intensity, (byte)0, (byte)15));
        Assert.NotNull(frame.Mask);
    }
}
