using DmdClock.Core.Scn;

namespace DmdClock.Core.Clock;

public static class DotClkDmdFrameFactory
{
    public static DmdFrame Create(string text, DotClkFont font)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(font);

        var glyphs = text.Select(character => CreateGlyph(font, character)).ToArray();
        var textWidth = glyphs.Length == 0
            ? 0
            : glyphs.Sum(glyph => glyph.Width) - glyphs[..^1].Sum(glyph => glyph.Kerning);
        var intensities = new byte[ScnReader.DisplayWidth * ScnReader.DisplayHeight];
        var mask = Enumerable.Repeat((byte)1, intensities.Length).ToArray();
        var cursorX = (ScnReader.DisplayWidth - textWidth) / 2;
        var top = (ScnReader.DisplayHeight - font.Height) / 2;
        var previousGlyphEnd = int.MinValue;

        foreach (var glyph in glyphs)
        {
            BlitGlyph(glyph, cursorX, top, previousGlyphEnd, intensities, mask);
            previousGlyphEnd = cursorX + glyph.Width;
            cursorX += glyph.Width - glyph.Kerning;
        }

        return new DmdFrame(ScnReader.DisplayWidth, ScnReader.DisplayHeight, intensities, mask);
    }

    private static void BlitGlyph(
        RenderGlyph glyph,
        int left,
        int top,
        int previousGlyphEnd,
        byte[] destination,
        byte[] destinationMask)
    {
        for (var y = 0; y < glyph.Height; y++)
        for (var x = 0; x < glyph.Width; x++)
        {
            var destinationX = left + x;
            var destinationY = top + y;
            if (destinationX < 0 || destinationX >= ScnReader.DisplayWidth ||
                destinationY < 0 || destinationY >= ScnReader.DisplayHeight)
                continue;

            var sourceOffset = (y * glyph.Width) + x;
            var destinationOffset = (destinationY * ScnReader.DisplayWidth) + destinationX;
            destination[destinationOffset] = glyph.Intensities[sourceOffset];
            destinationMask[destinationOffset] = destinationX < previousGlyphEnd
                ? (byte)(destinationMask[destinationOffset] & glyph.Mask[sourceOffset])
                : glyph.Mask[sourceOffset];
        }
    }

    private static RenderGlyph CreateGlyph(DotClkFont font, char character)
    {
        var native = font.FindGlyph(char.ToUpperInvariant(character));
        if (native is not null)
        {
            var intensities = new byte[native.Width * font.Height];
            var mask = new byte[intensities.Length];
            for (var y = 0; y < font.Height; y++)
            {
                Array.Copy(font.Intensities, (y * font.AtlasWidth) + native.Offset,
                    intensities, y * native.Width, native.Width);
                Array.Copy(font.Mask, (y * font.AtlasWidth) + native.Offset,
                    mask, y * native.Width, native.Width);
            }
            return new RenderGlyph(native.Width, font.Height, native.Kerning, intensities, mask);
        }

        return CreateFallbackGlyph(character, font.Height);
    }

    private static RenderGlyph CreateFallbackGlyph(char character, int height)
    {
        var width = character switch
        {
            ' ' => Math.Max(3, height / 4),
            '.' => 3,
            '-' => Math.Max(5, height / 3),
            '/' => Math.Max(7, height / 2),
            _ => Math.Max(3, height / 4)
        };
        var intensities = new byte[width * height];
        var mask = Enumerable.Repeat((byte)1, intensities.Length).ToArray();

        void Light(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            var offset = (y * width) + x;
            intensities[offset] = 15;
            mask[offset] = 0;
        }

        switch (character)
        {
            case '-':
                for (var x = 1; x < width - 1; x++) Light(x, height / 2);
                break;
            case '.':
                Light(width / 2, height - 2);
                if (height >= 18) Light(width / 2, height - 3);
                break;
            case '/':
                for (var y = 1; y < height - 1; y++)
                {
                    var x = width - 2 - (int)Math.Round((width - 3d) * y / Math.Max(1, height - 2));
                    Light(x, y);
                }
                break;
        }

        return new RenderGlyph(width, height, Kerning: 1, intensities, mask);
    }

    private sealed record RenderGlyph(int Width, int Height, int Kerning, byte[] Intensities, byte[] Mask);
}
