using System.Globalization;

namespace DmdClock.Core.Colorization;

public static class SyntheticFrameDumpReader
{
    public const int SupportedVersion = 1;

    public static DmdFrameDump Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static DmdFrameDump Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("The frame dump stream must be readable.", nameof(stream));

        using var reader = new StreamReader(stream, leaveOpen: true);
        var lineNumber = 0;
        var header = ReadContentLine(reader, ref lineNumber)
            ?? throw new FrameDumpFormatException("Frame dump is empty.");
        var headerParts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (headerParts.Length != 5 || headerParts[0] != "DMD-DUMP")
            throw Error(lineNumber, "Expected 'DMD-DUMP <version> width=<n> height=<n> bpp=<n>'.");

        var version = ParsePositiveInt(headerParts[1], "version", lineNumber);
        if (version != SupportedVersion)
            throw Error(lineNumber, $"Unsupported frame dump version {version}; expected {SupportedVersion}.");
        var width = ParseProperty(headerParts[2], "width", lineNumber);
        var height = ParseProperty(headerParts[3], "height", lineNumber);
        var bitsPerPixel = ParseProperty(headerParts[4], "bpp", lineNumber);
        if (bitsPerPixel is not (2 or 4))
            throw Error(lineNumber, "Synthetic source frames must use 2 or 4 bits per pixel.");
        if (width > 4096 || height > 4096 || (long)width * height > 16_777_216)
            throw Error(lineNumber, "Frame dimensions exceed the synthetic dump safety limit.");

        var frames = new List<DmdDumpFrame>();
        long previousTimestamp = -1;
        while (ReadContentLine(reader, ref lineNumber) is { } frameHeader)
        {
            var timestampText = ParseTextProperty(frameHeader, "FRAME timestampMs", lineNumber);
            if (!long.TryParse(timestampText, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampMs) ||
                timestampMs < 0)
                throw Error(lineNumber, "timestampMs must be a non-negative integer.");
            if (timestampMs < previousTimestamp)
                throw Error(lineNumber, "Frame timestamps must be in ascending order.");
            previousTimestamp = timestampMs;

            var intensities = new byte[checked(width * height)];
            var maximumValue = (1 << bitsPerPixel) - 1;
            for (var y = 0; y < height; y++)
            {
                var row = reader.ReadLine();
                lineNumber++;
                if (row is null)
                    throw Error(lineNumber, "Frame ended before all pixel rows were read.");
                row = row.Trim();
                if (row.Length != width)
                    throw Error(lineNumber, $"Pixel row must contain exactly {width} hexadecimal values.");
                for (var x = 0; x < width; x++)
                {
                    var value = HexValue(row[x]);
                    if (value < 0 || value > maximumValue)
                        throw Error(lineNumber, $"Pixel '{row[x]}' is outside the {bitsPerPixel}-bit source range.");
                    intensities[(y * width) + x] = (byte)value;
                }
            }

            frames.Add(new DmdDumpFrame(TimeSpan.FromMilliseconds(timestampMs), new DmdFrame(width, height, intensities)));
        }

        if (frames.Count == 0)
            throw new FrameDumpFormatException("Frame dump must contain at least one frame.");
        return new DmdFrameDump(version, bitsPerPixel, frames);
    }

    private static string? ReadContentLine(StreamReader reader, ref int lineNumber)
    {
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            line = line.Trim();
            if (line.Length > 0 && !line.StartsWith('#')) return line;
        }
        return null;
    }

    private static int ParseProperty(string text, string name, int lineNumber) =>
        ParsePositiveInt(ParseTextProperty(text, name, lineNumber), name, lineNumber);

    private static string ParseTextProperty(string text, string name, int lineNumber)
    {
        var prefix = name + "=";
        if (!text.StartsWith(prefix, StringComparison.Ordinal) || text.Length == prefix.Length)
            throw Error(lineNumber, $"Expected property '{prefix}<value>'.");
        return text[prefix.Length..];
    }

    private static int ParsePositiveInt(string text, string name, int lineNumber)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)
            throw Error(lineNumber, $"{name} must be a positive integer.");
        return value;
    }

    private static int HexValue(char value) => value switch
    {
        >= '0' and <= '9' => value - '0',
        >= 'a' and <= 'f' => value - 'a' + 10,
        >= 'A' and <= 'F' => value - 'A' + 10,
        _ => -1
    };

    private static FrameDumpFormatException Error(int lineNumber, string message) =>
        new($"Line {lineNumber}: {message}");
}
