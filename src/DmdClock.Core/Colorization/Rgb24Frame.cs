namespace DmdClock.Core.Colorization;

public sealed class Rgb24Frame
{
    public Rgb24Frame(int width, int height, byte[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(pixels);

        var expectedLength = checked(width * height * 3);
        if (pixels.Length != expectedLength)
            throw new ArgumentException($"RGB24 data must contain exactly {expectedLength} bytes.", nameof(pixels));

        Width = width;
        Height = height;
        Stride = checked(width * 3);
        Pixels = new ReadOnlyMemory<byte>((byte[])pixels.Clone());
    }

    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public ReadOnlyMemory<byte> Pixels { get; }

    public Rgb24 GetPixel(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
        var offset = checked((y * Stride) + (x * 3));
        var pixels = Pixels.Span;
        return new Rgb24(pixels[offset], pixels[offset + 1], pixels[offset + 2]);
    }
}
