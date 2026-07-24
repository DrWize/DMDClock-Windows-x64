using System.IO.Compression;
using System.Net.Http.Headers;

namespace DmdClock.Core.Library;

public sealed class ScenePackDownloader
{
    public const string SourcePageUrl =
        "https://github.com/sigmafx/DotClk-Resources/tree/master/Scenes";
    public const string SourceUrl =
        "https://github.com/sigmafx/DotClk-Resources/archive/refs/heads/master.zip";
    public const long MaximumDownloadBytes = 512L * 1024 * 1024;
    public const long MaximumExtractedBytes = 2L * 1024 * 1024 * 1024;
    public const int MaximumSceneCount = 20_000;

    private readonly HttpClient _httpClient;

    public ScenePackDownloader(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public async Task<ScenePackInstallResult> DownloadAndInstallAsync(
        string destinationDirectory,
        IProgress<ScenePackDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default,
        string? metadataPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        var temporaryArchive = Path.Combine(
            Path.GetTempPath(), $"dmdclock-scenes-{Guid.NewGuid():N}.zip");
        long downloadedBytes = 0;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, SourceUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DMDClock", "1.0"));
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            if (totalBytes > MaximumDownloadBytes)
                throw new InvalidDataException(
                    $"The scene archive is larger than the {MaximumDownloadBytes / 1024 / 1024} MB safety limit.");

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false))
            await using (var target = new FileStream(
                temporaryArchive, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[81920];
                while (true)
                {
                    var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                    downloadedBytes += read;
                    if (downloadedBytes > MaximumDownloadBytes)
                        throw new InvalidDataException(
                            $"The scene archive exceeded the {MaximumDownloadBytes / 1024 / 1024} MB safety limit.");
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    progress?.Report(new ScenePackDownloadProgress(
                        downloadedBytes,
                        totalBytes,
                        totalBytes is > 0
                            ? (int)Math.Min(100, downloadedBytes * 100 / totalBytes.Value)
                            : null));
                }
            }

            var sceneCount = await ExtractScenesAtomicallyAsync(
                temporaryArchive, destinationDirectory, cancellationToken, metadataPath)
                .ConfigureAwait(false);
            return new ScenePackInstallResult(
                Path.GetFullPath(destinationDirectory), sceneCount, downloadedBytes);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryArchive)) File.Delete(temporaryArchive);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static async Task<int> ExtractScenesAtomicallyAsync(
        string archivePath,
        string destinationDirectory,
        CancellationToken cancellationToken = default,
        string? metadataPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        var destination = Path.GetFullPath(destinationDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetDirectoryName(destination)
            ?? throw new ArgumentException("The destination must have a parent directory.", nameof(destinationDirectory));
        Directory.CreateDirectory(parent);

        var name = Path.GetFileName(destination);
        var staging = Path.Combine(parent, $".{name}-download-{Guid.NewGuid():N}");
        var backup = Path.Combine(parent, $".{name}-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);

        var destinationReplaced = false;
        try
        {
            var stagingPrefix = staging + Path.DirectorySeparatorChar;
            var sceneCount = 0;
            long extractedBytes = 0;

            using var archive = ZipFile.OpenRead(archivePath);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var normalizedName = entry.FullName.Replace('\\', '/');
                var scenesMarker = normalizedName.IndexOf("/Scenes/", StringComparison.OrdinalIgnoreCase);
                if (scenesMarker < 0 ||
                    !normalizedName.EndsWith(".scn", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relativeName = normalizedName[(scenesMarker + "/Scenes/".Length)..];
                if (string.IsNullOrWhiteSpace(relativeName)) continue;

                sceneCount++;
                if (sceneCount > MaximumSceneCount)
                    throw new InvalidDataException(
                        $"The archive contains more than {MaximumSceneCount:N0} scene files.");

                extractedBytes += entry.Length;
                if (extractedBytes > MaximumExtractedBytes)
                    throw new InvalidDataException(
                        $"The extracted scenes exceed the {MaximumExtractedBytes / 1024 / 1024} MB safety limit.");

                var targetPath = Path.GetFullPath(Path.Combine(
                    staging, relativeName.Replace('/', Path.DirectorySeparatorChar)));
                if (!targetPath.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Unsafe scene path in archive: {entry.FullName}");

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                await using var source = entry.Open();
                await using var target = new FileStream(
                    targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    81920, FileOptions.Asynchronous);
                await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            }

            if (sceneCount == 0)
                throw new InvalidDataException("The downloaded archive did not contain any DotClk .scn files.");

            if (!string.IsNullOrWhiteSpace(metadataPath))
            {
                var metadataSource = Path.GetFullPath(metadataPath);
                if (!File.Exists(metadataSource))
                    throw new FileNotFoundException("Scene metadata file was not found.", metadataSource);

                await using var source = new FileStream(
                    metadataSource, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var target = new FileStream(
                    Path.Combine(staging, SceneMetadataStore.DefaultFileName),
                    FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    81920, FileOptions.Asynchronous);
                await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            }

            if (Directory.Exists(destination))
            {
                Directory.Move(destination, backup);
                destinationReplaced = true;
            }
            Directory.Move(staging, destination);
            if (Directory.Exists(backup)) Directory.Delete(backup, recursive: true);
            return sceneCount;
        }
        catch
        {
            if (destinationReplaced && !Directory.Exists(destination) && Directory.Exists(backup))
                Directory.Move(backup, destination);
            throw;
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
            if (Directory.Exists(backup) && Directory.Exists(destination))
                Directory.Delete(backup, recursive: true);
        }
    }
}
