namespace DmdClock.Core.Library;

public sealed record AnimationLibraryItem(
    string Id,
    string RelativePath,
    long FileSize,
    DateTimeOffset LastWriteUtc,
    string Sha256,
    int FrameCount,
    long EstimatedDurationMs,
    string? Error)
{
    public bool IsValid => Error is null;
}

