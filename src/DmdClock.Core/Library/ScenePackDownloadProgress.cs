namespace DmdClock.Core.Library;

public sealed record ScenePackDownloadProgress(
    long BytesDownloaded,
    long? TotalBytes,
    int? Percentage);
