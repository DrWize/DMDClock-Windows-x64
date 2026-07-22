using DmdClock.Core;
using DmdClock.Core.Scn;
using SkiaSharp;

namespace DmdClock.App.Rendering;

public static class OpenTypeDmdFrameFactory
{
    public static DmdFrame Create(string text, string fontPath)
    {
        using var typeface = SKTypeface.FromFile(fontPath)
            ?? throw new InvalidDataException($"Unable to load font: {fontPath}");
        const int width = ScnReader.DisplayWidth;
        const int height = ScnReader.DisplayHeight;
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var font = new SKFont(typeface, 30);

        canvas.Clear(SKColors.Transparent);
        var bounds = new SKRect();
        font.MeasureText(text, out bounds, paint);
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            var scale = Math.Min((width - 2f) / bounds.Width, (height - 2f) / bounds.Height);
            font.Size *= Math.Min(1f, scale);
            font.MeasureText(text, out bounds, paint);
            var x = (width - bounds.Width) / 2f - bounds.Left;
            var y = (height - bounds.Height) / 2f - bounds.Top;
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
            canvas.Flush();
        }

        var intensities = new byte[width * height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            intensities[(y * width) + x] = (byte)Math.Clamp((bitmap.GetPixel(x, y).Alpha + 8) / 17, 0, 15);
        return new DmdFrame(width, height, intensities);
    }
}
