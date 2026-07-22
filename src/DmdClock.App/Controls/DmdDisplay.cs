using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DmdClock.Core;
using DmdClock.Core.Settings;

namespace DmdClock.App.Controls;

public sealed class DmdDisplay : Control
{
    private IBrush[] _dotBrushes = [];
    private IBrush[] _glowBrushes = [];
    private DmdFrame? _frame;
    private bool _glowEnabled = true;
    private IBrush _backgroundBrush = Brushes.Black;

    public DmdDisplay() => SetAppearance(DmdColorPreset.Orange, 100, true, null, "#000000");

    public void SetAppearance(DmdColorPreset preset, int brightnessPercent, bool glowEnabled,
        string? foregroundColor, string? backgroundColor)
    {
        brightnessPercent = Math.Clamp(brightnessPercent, 25, 100);
        var presetColor = preset switch
        {
            DmdColorPreset.Red => Color.FromRgb(255, 32, 16),
            DmdColorPreset.Plasma => Color.FromRgb(120, 100, 255),
            DmdColorPreset.Monochrome => Color.FromRgb(235, 235, 235),
            _ => Color.FromRgb(255, 112, 14)
        };
        var color = ParseColor(foregroundColor, presetColor);
        _backgroundBrush = new SolidColorBrush(ParseColor(backgroundColor, Colors.Black));
        _dotBrushes = CreateDotBrushes(color, brightnessPercent / 100d);
        _glowBrushes = CreateGlowBrushes(color, brightnessPercent / 100d);
        _glowEnabled = glowEnabled;
        InvalidateVisual();
    }

    public DmdFrame? Frame
    {
        get => _frame;
        set
        {
            _frame = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(_backgroundBrush, Bounds);

        var frame = Frame;
        if (frame is null)
            return;

        var cellSize = Math.Min(Bounds.Width / frame.Width, Bounds.Height / frame.Height);
        var displayWidth = cellSize * frame.Width;
        var displayHeight = cellSize * frame.Height;
        var originX = (Bounds.Width - displayWidth) / 2;
        var originY = (Bounds.Height - displayHeight) / 2;
        var radius = cellSize * 0.34;
        var glowRadius = cellSize * 0.47;
        var pixels = frame.Intensities.Span;

        for (var y = 0; y < frame.Height; y++)
        for (var x = 0; x < frame.Width; x++)
        {
            var intensity = pixels[(y * frame.Width) + x];
            if (intensity == 0)
                continue;

            var center = new Point(originX + ((x + 0.5) * cellSize), originY + ((y + 0.5) * cellSize));
            if (_glowEnabled)
                context.DrawEllipse(_glowBrushes[intensity], null, center, glowRadius, glowRadius);
            context.DrawEllipse(_dotBrushes[intensity], null, center, radius, radius);
        }
    }

    private static IBrush[] CreateDotBrushes(Color color, double brightness) => Enumerable.Range(0, 16)
        .Select(level =>
        {
            var factor = (0.12 + (0.88 * level / 15d)) * brightness;
            return (IBrush)new SolidColorBrush(Color.FromRgb(
                Scale(color.R, factor),
                Scale(color.G, factor),
                Scale(color.B, factor)));
        })
        .ToArray();

    private static IBrush[] CreateGlowBrushes(Color color, double brightness) => Enumerable.Range(0, 16)
        .Select(level => (IBrush)new SolidColorBrush(Color.FromArgb(
            Scale(72, level / 15d * brightness), color.R, color.G, color.B)))
        .ToArray();

    private static byte Scale(byte value, double factor) => (byte)Math.Clamp(Math.Round(value * factor), 0, 255);

    private static Color ParseColor(string? value, Color fallback)
    {
        try { return string.IsNullOrWhiteSpace(value) ? fallback : Color.Parse(value); }
        catch (FormatException) { return fallback; }
    }
}
