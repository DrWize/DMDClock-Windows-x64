using System.Text;

namespace DmdClock.Core.Clock;

public static class DotClkFontReader
{
    private const ushort SupportedVersion = 1;
    private const ushort SupportedBitsPerPixel = 4;
    private const int MaximumGlyphs = 256;
    private const int MaximumAtlasWidth = 4096;
    private const int MaximumHeight = 32;

    public static DotClkFont Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) throw new ArgumentException("The font stream must be readable.", nameof(stream));

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var version = reader.ReadUInt16();
            if (version != SupportedVersion)
                throw new InvalidDataException($"Unsupported DotClk font version {version}.");

            var name = reader.ReadString();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException("The DotClk font name is empty.");

            var glyphCount = reader.ReadUInt16();
            if (glyphCount is 0 or > MaximumGlyphs)
                throw new InvalidDataException($"Invalid DotClk glyph count {glyphCount}.");

            var glyphData = new (char Character, int Width, int Kerning)[glyphCount];
            var seenCharacters = new HashSet<char>();
            var expectedAtlasWidth = 0;
            for (var index = 0; index < glyphCount; index++)
            {
                var character = reader.ReadChar();
                var width = reader.ReadUInt16();
                var kerning = reader.ReadUInt16();
                if (width == 0 || kerning >= width)
                    throw new InvalidDataException(
                        $"Invalid metrics for DotClk glyph '{character}': width {width}, kerning {kerning}.");
                if (!seenCharacters.Add(character))
                    throw new InvalidDataException($"Duplicate DotClk glyph '{character}'.");
                expectedAtlasWidth = checked(expectedAtlasWidth + width);
                glyphData[index] = (character, width, kerning);
            }

            var atlasWidth = reader.ReadUInt16();
            var height = reader.ReadUInt16();
            var bitsPerPixel = reader.ReadUInt16();
            var hasMask = reader.ReadUInt16();
            if (atlasWidth is 0 or > MaximumAtlasWidth || atlasWidth != expectedAtlasWidth)
                throw new InvalidDataException(
                    $"Invalid DotClk atlas width {atlasWidth}; glyphs require {expectedAtlasWidth}.");
            if (height is 0 or > MaximumHeight)
                throw new InvalidDataException($"Invalid DotClk font height {height}.");
            if (bitsPerPixel != SupportedBitsPerPixel)
                throw new InvalidDataException($"Unsupported DotClk pixel depth {bitsPerPixel}.");
            if (hasMask is not (0 or 1))
                throw new InvalidDataException($"Invalid DotClk mask flag {hasMask}.");

            var intensities = new byte[atlasWidth * height];
            var dotBytesPerRow = (atlasWidth + 1) / 2;
            for (var y = 0; y < height; y++)
            for (var packedX = 0; packedX < dotBytesPerRow; packedX++)
            {
                var packed = reader.ReadByte();
                var x = packedX * 2;
                intensities[(y * atlasWidth) + x] = (byte)(packed & 0x0f);
                if (x + 1 < atlasWidth)
                    intensities[(y * atlasWidth) + x + 1] = (byte)((packed >> 4) & 0x0f);
            }

            var mask = Enumerable.Repeat((byte)1, atlasWidth * height).ToArray();
            if (hasMask == 1)
            {
                var maskBytesPerRow = (atlasWidth + 7) / 8;
                for (var y = 0; y < height; y++)
                for (var packedX = 0; packedX < maskBytesPerRow; packedX++)
                {
                    var packed = reader.ReadByte();
                    for (var bit = 0; bit < 8; bit++)
                    {
                        var x = (packedX * 8) + bit;
                        if (x < atlasWidth)
                            mask[(y * atlasWidth) + x] = (byte)((packed >> bit) & 1);
                    }
                }
            }
            else
            {
                for (var index = 0; index < mask.Length; index++)
                    mask[index] = intensities[index] == 0 ? (byte)1 : (byte)0;
            }

            var glyphs = new List<DotClkGlyph>(glyphCount);
            var offset = 0;
            foreach (var glyph in glyphData)
            {
                glyphs.Add(new DotClkGlyph(glyph.Character, glyph.Width, glyph.Kerning, offset));
                offset += glyph.Width;
            }

            return new DotClkFont(name, glyphs, atlasWidth, height, intensities, mask);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("The DotClk font is truncated.", exception);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("The DotClk font contains invalid character data.", exception);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException("The DotClk font dimensions are too large.", exception);
        }
    }
}
