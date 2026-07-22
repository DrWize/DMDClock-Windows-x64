using System.Buffers.Binary;

namespace DmdClock.Core.Scn;

public static class ScnReader
{
    public const ushort SupportedVersion = 1;
    public const int DisplayWidth = 128;
    public const int DisplayHeight = 32;
    private const int BitsPerPixel = 4;
    private const int StoryboardReservedBytes = 17;
    private const int MaxItemCount = 10_000;

    public static ScnScene Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static ScnScene Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("The SCN stream must be readable.", nameof(stream));

        try
        {
            var version = ReadUInt16(stream);
            if (version != SupportedVersion)
                throw new ScnFormatException($"Unsupported SCN version {version}; expected {SupportedVersion}.");

            var frameCount = ReadUInt16(stream);
            var storyboardCount = ReadUInt16(stream);
            ValidateCount(frameCount, "frame");
            ValidateCount(storyboardCount, "storyboard");

            var storyboards = new List<ScnStoryboard>(storyboardCount);
            for (var index = 0; index < storyboardCount; index++)
                storyboards.Add(ReadStoryboard(stream));

            var frames = new List<DmdFrame>(frameCount);
            for (var index = 0; index < frameCount; index++)
                frames.Add(ReadFrame(stream, index));

            if (stream.CanSeek && stream.Position != stream.Length)
                throw new ScnFormatException($"SCN contains {stream.Length - stream.Position} unexpected trailing bytes.");

            return new ScnScene(version, storyboards, frames);
        }
        catch (EndOfStreamException exception)
        {
            throw new ScnFormatException("SCN ended before all declared data could be read.", exception);
        }
    }

    private static ScnStoryboard ReadStoryboard(Stream stream)
    {
        var storyboard = new ScnStoryboard(
            ReadUInt16(stream),
            ReadFlag(stream, "first-frame clock layer"),
            ReadFlag(stream, "first-frame blank flag"),
            ReadUInt16(stream),
            ReadFlag(stream, "frame clock layer"),
            ReadUInt16(stream),
            ReadFlag(stream, "last-frame clock layer"),
            ReadFlag(stream, "last-frame blank flag"),
            ReadByte(stream),
            ReadByte(stream),
            ReadByte(stream));

        SkipExactly(stream, StoryboardReservedBytes);
        return storyboard;
    }

    private static DmdFrame ReadFrame(Stream stream, int frameIndex)
    {
        var width = ReadUInt16(stream);
        var height = ReadUInt16(stream);
        var bitsPerPixel = ReadUInt16(stream);
        var hasMask = ReadFlag(stream, $"frame {frameIndex} mask flag");

        if (width != DisplayWidth || height != DisplayHeight || bitsPerPixel != BitsPerPixel)
            throw new ScnFormatException(
                $"Frame {frameIndex} has unsupported geometry {width}x{height} at {bitsPerPixel} bpp; " +
                $"expected {DisplayWidth}x{DisplayHeight} at {BitsPerPixel} bpp.");

        var pixelCount = checked((int)width * height);
        var intensities = new byte[pixelCount];
        for (var offset = 0; offset < pixelCount; offset += 2)
        {
            var packed = ReadByte(stream);
            intensities[offset] = (byte)(packed & 0x0f);
            intensities[offset + 1] = (byte)(packed >> 4);
        }

        byte[]? mask = null;
        if (hasMask)
        {
            mask = new byte[pixelCount];
            for (var offset = 0; offset < pixelCount; offset += 8)
            {
                var packed = ReadByte(stream);
                for (var bit = 0; bit < 8; bit++)
                    mask[offset + bit] = (byte)((packed >> bit) & 1);
            }
        }

        return new DmdFrame(width, height, intensities, mask);
    }

    private static bool ReadFlag(Stream stream, string fieldName)
    {
        var value = ReadUInt16(stream);
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new ScnFormatException($"Invalid {fieldName} value {value}; expected 0 or 1.")
        };
    }

    private static ushort ReadUInt16(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    private static byte ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        return value < 0 ? throw new EndOfStreamException() : (byte)value;
    }

    private static void SkipExactly(Stream stream, int count)
    {
        Span<byte> buffer = stackalloc byte[count];
        stream.ReadExactly(buffer);
    }

    private static void ValidateCount(ushort count, string itemName)
    {
        if (count > MaxItemCount)
            throw new ScnFormatException($"SCN declares too many {itemName} items: {count}.");
    }
}

