namespace DmdClock.Core.Clock;

public sealed class DotClkFont
{
    internal DotClkFont(
        string name,
        IReadOnlyList<DotClkGlyph> glyphs,
        int atlasWidth,
        int height,
        byte[] intensities,
        byte[] mask)
    {
        Name = name;
        Glyphs = glyphs;
        AtlasWidth = atlasWidth;
        Height = height;
        Intensities = intensities;
        Mask = mask;
    }

    public string Name { get; }
    public IReadOnlyList<DotClkGlyph> Glyphs { get; }
    public int AtlasWidth { get; }
    public int Height { get; }
    internal byte[] Intensities { get; }
    internal byte[] Mask { get; }

    public bool Supports(char character) => Glyphs.Any(glyph => glyph.Character == character);

    internal DotClkGlyph? FindGlyph(char character) =>
        Glyphs.FirstOrDefault(glyph => glyph.Character == character);
}

public sealed record DotClkGlyph(char Character, int Width, int Kerning, int Offset);
