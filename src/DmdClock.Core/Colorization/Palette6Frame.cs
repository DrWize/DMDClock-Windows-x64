namespace DmdClock.Core.Colorization;

public sealed class Palette6Frame
{
    public const int MaximumColors = 64;

    public Palette6Frame(int width, int height, byte[] indices, IReadOnlyList<Rgb24> palette)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentNullException.ThrowIfNull(palette);

        var pixelCount = checked(width * height);
        if (indices.Length != pixelCount)
            throw new ArgumentException("Palette indices must contain one value per pixel.", nameof(indices));
        if (palette.Count is < 1 or > MaximumColors)
            throw new ArgumentException($"A 6-bit palette must contain between 1 and {MaximumColors} colors.", nameof(palette));
        if (indices.Any(index => index >= palette.Count))
            throw new ArgumentException("Every palette index must refer to an available color.", nameof(indices));

        Width = width;
        Height = height;
        Indices = new ReadOnlyMemory<byte>((byte[])indices.Clone());
        Palette = Array.AsReadOnly(palette.ToArray());
    }

    public int Width { get; }
    public int Height { get; }
    public ReadOnlyMemory<byte> Indices { get; }
    public IReadOnlyList<Rgb24> Palette { get; }

    public Rgb24Frame ToRgb24()
    {
        var output = new byte[checked(Width * Height * 3)];
        var indices = Indices.Span;
        for (var index = 0; index < indices.Length; index++)
        {
            var color = Palette[indices[index]];
            var outputOffset = index * 3;
            output[outputOffset] = color.Red;
            output[outputOffset + 1] = color.Green;
            output[outputOffset + 2] = color.Blue;
        }

        return new Rgb24Frame(Width, Height, output);
    }
}
