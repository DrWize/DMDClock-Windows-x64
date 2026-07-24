using System.Text.Json;
using System.Text.Json.Serialization;

namespace DmdClock.Core.Library;

public sealed class AnimationSelectionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<AnimationSelectionDocument> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return AnimationSelectionDocument.Empty;
        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var document = await JsonSerializer.DeserializeAsync<AnimationSelectionDocument>(
                stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            return document?.SchemaVersion == AnimationSelectionDocument.CurrentSchemaVersion
                ? document.Normalize()
                : AnimationSelectionDocument.Empty;
        }
        catch (Exception exception) when (
            exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return AnimationSelectionDocument.Empty;
        }
    }

    public async Task SaveAtomicAsync(
        AnimationSelectionDocument document,
        string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Selection path needs a directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream, document.Normalize(), JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
