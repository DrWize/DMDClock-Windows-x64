using DmdClock.Core.Screensaver;

namespace DmdClock.Core.Tests.Screensaver;

public sealed class ScreenSaverLaunchOptionsTests
{
    [Theory]
    [InlineData("/s", ScreenSaverLaunchMode.Fullscreen)]
    [InlineData("-S", ScreenSaverLaunchMode.Fullscreen)]
    [InlineData("/c", ScreenSaverLaunchMode.Configure)]
    [InlineData("/c:123", ScreenSaverLaunchMode.Configure)]
    [InlineData("/review", ScreenSaverLaunchMode.Reviewer)]
    public void Parse_RecognizesStandardModes(string argument, ScreenSaverLaunchMode expected)
    {
        var options = ScreenSaverLaunchOptions.Parse([argument], "DMDClock.scr");
        Assert.Equal(expected, options.Mode);
    }

    [Theory]
    [InlineData("/p", "12345")]
    [InlineData("/p:12345", null)]
    public void Parse_RecognizesPreviewParent(string argument, string? secondArgument)
    {
        var args = secondArgument is null ? new[] { argument } : new[] { argument, secondArgument };
        var options = ScreenSaverLaunchOptions.Parse(args, "DMDClock.scr");
        Assert.Equal(ScreenSaverLaunchMode.Preview, options.Mode);
        Assert.Equal((nint)12345, options.PreviewParent);
    }

    [Fact]
    public void Parse_UsesExecutableExtensionWhenNoArgumentsAreProvided()
    {
        Assert.Equal(ScreenSaverLaunchMode.Configure,
            ScreenSaverLaunchOptions.Parse([], "DMDClock.scr").Mode);
        Assert.Equal(ScreenSaverLaunchMode.Normal,
            ScreenSaverLaunchOptions.Parse([], "DmdClock.App.exe").Mode);
    }

    [Fact]
    public void Parse_InvalidPreviewFallsBackToConfiguration()
    {
        Assert.Equal(ScreenSaverLaunchMode.Configure,
            ScreenSaverLaunchOptions.Parse(["/p", "invalid"], "DMDClock.scr").Mode);
    }
}
