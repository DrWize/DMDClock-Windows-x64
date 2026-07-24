namespace DmdClock.Core.Screensaver;

public enum ScreenSaverLaunchMode
{
    Normal,
    Fullscreen,
    Configure,
    Preview,
    Reviewer
}

public sealed record ScreenSaverLaunchOptions(ScreenSaverLaunchMode Mode, nint PreviewParent)
{
    public static ScreenSaverLaunchOptions Parse(IReadOnlyList<string> args, string? executablePath)
    {
        if (args.Count == 0)
        {
            var extension = Path.GetExtension(executablePath ?? string.Empty);
            return new(extension.Equals(".scr", StringComparison.OrdinalIgnoreCase)
                ? ScreenSaverLaunchMode.Configure
                : ScreenSaverLaunchMode.Normal, 0);
        }

        var first = args[0].Trim();
        var separator = first.IndexOf(':');
        var command = (separator >= 0 ? first[..separator] : first).TrimStart('/', '-').ToLowerInvariant();
        var inlineValue = separator >= 0 ? first[(separator + 1)..] : null;
        return command switch
        {
            "s" => new(ScreenSaverLaunchMode.Fullscreen, 0),
            "c" => new(ScreenSaverLaunchMode.Configure, 0),
            "p" => ParsePreview(inlineValue ?? args.ElementAtOrDefault(1)),
            "review" => new(ScreenSaverLaunchMode.Reviewer, 0),
            _ => new(ScreenSaverLaunchMode.Configure, 0)
        };
    }

    private static ScreenSaverLaunchOptions ParsePreview(string? value) =>
        long.TryParse(value, out var handle) && handle > 0
            ? new(ScreenSaverLaunchMode.Preview, (nint)handle)
            : new(ScreenSaverLaunchMode.Configure, 0);
}
