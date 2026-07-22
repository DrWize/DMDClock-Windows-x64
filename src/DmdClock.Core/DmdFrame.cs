namespace DmdClock.Core;

public sealed class DmdFrame
{
    public DmdFrame(int width, int height, byte[] intensities, byte[]? mask = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(intensities);

        var pixelCount = checked(width * height);
        if (intensities.Length != pixelCount)
            throw new ArgumentException("Intensity data must contain one value per pixel.", nameof(intensities));
        if (mask is not null && mask.Length != pixelCount)
            throw new ArgumentException("Mask data must contain one value per pixel.", nameof(mask));
        if (intensities.Any(static value => value > 15))
            throw new ArgumentException("Four-bit intensity values must be between 0 and 15.", nameof(intensities));

        Width = width;
        Height = height;
        Intensities = new ReadOnlyMemory<byte>(intensities);
        Mask = mask is null ? (ReadOnlyMemory<byte>?)null : new ReadOnlyMemory<byte>(mask);
    }

    public int Width { get; }
    public int Height { get; }
    public ReadOnlyMemory<byte> Intensities { get; }
    public ReadOnlyMemory<byte>? Mask { get; }

    public byte GetIntensity(int x, int y) => Intensities.Span[GetOffset(x, y)];

    public bool IsMasked(int x, int y) => Mask is { } mask && mask.Span[GetOffset(x, y)] != 0;

    private int GetOffset(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
        return checked((y * Width) + x);
    }
}
