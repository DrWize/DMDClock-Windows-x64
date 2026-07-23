using Avalonia;
using System;
using DmdClock.Core.Screensaver;

namespace DmdClock.App;

class Program
{
    internal static ScreenSaverLaunchOptions LaunchOptions { get; private set; } =
        new(ScreenSaverLaunchMode.Normal, 0);

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        LaunchOptions = ScreenSaverLaunchOptions.Parse(args, Environment.ProcessPath);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
