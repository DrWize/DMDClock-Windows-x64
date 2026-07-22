namespace DmdClock.Core.Colorization;

public sealed class ComparisonMask
{
    public ComparisonMask(int width, int height, byte[] ignoredPixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(ignoredPixels);
        if (ignoredPixels.Length != checked(width * height))
            throw new ArgumentException("Comparison mask must contain one value per pixel.", nameof(ignoredPixels));
        if (ignoredPixels.Any(static value => value is not (0 or 1)))
            throw new ArgumentException("Comparison mask values must be 0 (compare) or 1 (ignore).", nameof(ignoredPixels));

        Width = width;
        Height = height;
        IgnoredPixels = new ReadOnlyMemory<byte>((byte[])ignoredPixels.Clone());
    }

    public int Width { get; }
    public int Height { get; }
    public ReadOnlyMemory<byte> IgnoredPixels { get; }

    public bool IsIgnored(int offset) => IgnoredPixels.Span[offset] != 0;

    public static ComparisonMask CompareAll(int width, int height) =>
        new(width, height, new byte[checked(width * height)]);
}
