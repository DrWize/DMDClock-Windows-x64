using System.Collections.ObjectModel;
using System.Reflection;
using DmdClock.Core;
using DmdClock.Core.Clock;

namespace DmdClock.App.Rendering;

public static class EmbeddedDotClkFonts
{
    private const string IdPrefix = "DotClk/";
    private const string ResourcePrefix = "DmdClock.App.Assets.Fonts.DotClk.";
    private static readonly string[] FontNames = ["ALTERN8", "FISHY", "TREK", "TWILIGHT"];
    private static readonly IReadOnlyDictionary<string, Lazy<DotClkFont>> Fonts =
        new ReadOnlyDictionary<string, Lazy<DotClkFont>>(
            FontNames.ToDictionary(
                name => GetId(name),
                name => new Lazy<DotClkFont>(() => Load(name),
                    LazyThreadSafetyMode.ExecutionAndPublication),
                StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<string> Ids { get; } = FontNames.Select(GetId).ToArray();

    public static bool IsEmbedded(string? id) =>
        id is not null && Fonts.ContainsKey(id);

    public static DmdFrame Create(string text, string id)
    {
        if (!Fonts.TryGetValue(id, out var font))
            throw new ArgumentException($"Unknown embedded DotClk font '{id}'.", nameof(id));
        return DotClkDmdFrameFactory.Create(text, font.Value);
    }

    public static string GetDisplayName(string id) =>
        Path.GetFileNameWithoutExtension(id);

    private static string GetId(string name) => $"{IdPrefix}{name}.fnt";

    private static DotClkFont Load(string name)
    {
        var resourceName = $"{ResourcePrefix}{name}.fnt";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded DotClk resource '{resourceName}' is missing.");
        return DotClkFontReader.Read(stream);
    }
}
