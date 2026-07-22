namespace DmdClock.Core.Rendering;

public static class DmdFrameCompositor
{
    public static DmdFrame Compose(DmdFrame animation, DmdFrame clock, bool clockAbove)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(clock);
        if (animation.Width != clock.Width || animation.Height != clock.Height)
            throw new ArgumentException("Animation and clock frames must have identical dimensions.");

        var output = new byte[animation.Width * animation.Height];
        if (clockAbove)
        {
            Blit(output, animation);
            Blit(output, clock);
        }
        else
        {
            Blit(output, clock);
            Blit(output, animation);
        }

        return new DmdFrame(animation.Width, animation.Height, output);
    }

    public static DmdFrame CreateBlank(int width = 128, int height = 32) =>
        new(width, height, new byte[checked(width * height)], new byte[checked(width * height)]);

    private static void Blit(byte[] destination, DmdFrame source)
    {
        var pixels = source.Intensities.Span;
        var mask = source.Mask;
        if (pixels.Length != destination.Length)
            throw new ArgumentException($"Source contains {pixels.Length} pixels; expected {destination.Length}.");
        if (mask.HasValue && mask.Value.Length != destination.Length)
            throw new ArgumentException($"Source mask contains {mask.Value.Length} pixels; expected {destination.Length}.");
        for (var index = 0; index < destination.Length; index++)
        {
            if (mask.HasValue && mask.Value.Span[index] != 0)
                continue;
            destination[index] = pixels[index];
        }
    }
}
