namespace DmdClock.Core.Clock;

using System.Globalization;
using System.Text;

public static class ClockFrameFactory
{
    private const int GlyphHeight = 7;
    private const int GlyphSpacing = 1;
    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        ['0'] = ["11111", "10001", "10011", "10101", "11001", "10001", "11111"],
        ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        ['2'] = ["11111", "00001", "00001", "11111", "10000", "10000", "11111"],
        ['3'] = ["11111", "00001", "00001", "01111", "00001", "00001", "11111"],
        ['4'] = ["10001", "10001", "10001", "11111", "00001", "00001", "00001"],
        ['5'] = ["11111", "10000", "10000", "11111", "00001", "00001", "11111"],
        ['6'] = ["11111", "10000", "10000", "11111", "10001", "10001", "11111"],
        ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        ['8'] = ["11111", "10001", "10001", "11111", "10001", "10001", "11111"],
        ['9'] = ["11111", "10001", "10001", "11111", "00001", "00001", "11111"],
        ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['B'] = ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
        ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
        ['D'] = ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
        ['E'] = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        ['F'] = ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
        ['G'] = ["01111", "10000", "10000", "10111", "10001", "10001", "01111"],
        ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
        ['J'] = ["00111", "00010", "00010", "00010", "10010", "10010", "01100"],
        ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
        ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
        ['Q'] = ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
        ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
        ['W'] = ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
        ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
        [':'] = ["0", "0", "1", "0", "1", "0", "0"],
        ['-'] = ["000", "000", "000", "111", "000", "000", "000"],
        ['.'] = ["0", "0", "0", "0", "0", "0", "1"],
        ['/'] = ["00001", "00010", "00010", "00100", "01000", "01000", "10000"],
        [' '] = ["000", "000", "000", "000", "000", "000", "000"]
    };

    public static DmdFrame Create(DateTimeOffset time, bool twelveHour = false, bool showSeconds = true)
    {
        var format = (twelveHour, showSeconds) switch
        {
            (true, true) => "hh:mm:ss tt",
            (true, false) => "hh:mm tt",
            (false, true) => "HH:mm:ss",
            _ => "HH:mm"
        };
        return CreateText(time.ToString(format, CultureInfo.InvariantCulture), twelveHour ? 2 : 3, 64, 16);
    }

    public static DmdFrame CreateDate(DateTimeOffset time, string format = "yyyy-MM-dd") =>
        CreateText(time.ToString(format, CultureInfo.InvariantCulture), scale: 2, 64, 16);

    public static DmdFrame CreateCompactTime(DateTimeOffset time, int centerX, int centerY, bool twelveHour = false) =>
        CreateText(time.ToString(twelveHour ? "hh:mm" : "HH:mm", CultureInfo.InvariantCulture), scale: 2, centerX, centerY);

    public static DmdFrame CreateInformation(string? game, string? sequence)
    {
        var intensities = new byte[Scn.ScnReader.DisplayWidth * Scn.ScnReader.DisplayHeight];
        DrawText(intensities, FitInformationText(game, "OKANT SPEL"), scale: 1, centerX: 64, centerY: 9);
        DrawText(intensities, FitInformationText(sequence, "OKAND SEKVENS"), scale: 1, centerX: 64, centerY: 23);
        return CreateFrame(intensities);
    }

    private static DmdFrame CreateText(string text, int scale, int centerX, int centerY)
    {
        var intensities = new byte[Scn.ScnReader.DisplayWidth * Scn.ScnReader.DisplayHeight];
        DrawText(intensities, text, scale, centerX, centerY);
        return CreateFrame(intensities);
    }

    private static void DrawText(byte[] intensities, string text, int scale, int centerX, int centerY)
    {
        var logicalWidth = text.Sum(static character => Glyphs.GetValueOrDefault(character, Glyphs[' '])[0].Length) + ((text.Length - 1) * GlyphSpacing);
        var startX = centerX - ((logicalWidth * scale) / 2);
        var startY = centerY - ((GlyphHeight * scale) / 2);
        var cursorX = startX;

        foreach (var character in text)
        {
            var glyph = Glyphs.GetValueOrDefault(character, Glyphs[' ']);
            for (var glyphY = 0; glyphY < GlyphHeight; glyphY++)
            for (var glyphX = 0; glyphX < glyph[glyphY].Length; glyphX++)
            {
                if (glyph[glyphY][glyphX] != '1')
                    continue;

                for (var offsetY = 0; offsetY < scale; offsetY++)
                for (var offsetX = 0; offsetX < scale; offsetX++)
                {
                    var x = cursorX + (glyphX * scale) + offsetX;
                    var y = startY + (glyphY * scale) + offsetY;
                    if (x < 0 || x >= Scn.ScnReader.DisplayWidth || y < 0 || y >= Scn.ScnReader.DisplayHeight)
                        continue;
                    intensities[(y * Scn.ScnReader.DisplayWidth) + x] = 15;
                }
            }

            cursorX += (glyph[0].Length + GlyphSpacing) * scale;
        }
    }

    private static DmdFrame CreateFrame(byte[] intensities)
    {
        var mask = intensities.Select(static intensity => intensity == 0 ? (byte)1 : (byte)0).ToArray();
        return new DmdFrame(Scn.ScnReader.DisplayWidth, Scn.ScnReader.DisplayHeight, intensities, mask);
    }

    private static string FitInformationText(string? value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value;
        var normalized = source.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            var upper = char.ToUpperInvariant(character);
            builder.Append(Glyphs.ContainsKey(upper) ? upper : ' ');
        }

        var result = string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        const int maxCharacters = 21;
        return result.Length <= maxCharacters ? result : result[..(maxCharacters - 1)].TrimEnd() + "-";
    }
}
