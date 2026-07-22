using System.Text.Json;

namespace DmdClock.Core.Library;

public sealed class AnimationLibraryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<AnimationLibraryIndex?> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        var index = await JsonSerializer.DeserializeAsync<AnimationLibraryIndex>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        return index?.SchemaVersion == AnimationLibraryIndex.CurrentSchemaVersion ? index : null;
    }

    public async Task SaveAtomicAsync(AnimationLibraryIndex index, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(index);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("Index path needs a directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await JsonSerializer.SerializeAsync(stream, index, JsonOptions, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
