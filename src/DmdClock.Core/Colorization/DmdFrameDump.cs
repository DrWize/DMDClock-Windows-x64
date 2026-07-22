namespace DmdClock.Core.Colorization;

public sealed record DmdDumpFrame(TimeSpan Timestamp, DmdFrame Frame);

public sealed record DmdFrameDump(
    int Version,
    int SourceBitsPerPixel,
    IReadOnlyList<DmdDumpFrame> Frames);
