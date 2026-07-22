using System.Text.Json;

namespace DmdClock.Core.Settings;

public sealed class DmdClockSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<DmdClockSettings> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return DmdClockSettings.Default;
        try
        {
            await using var stream = File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync<DmdClockSettings>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return settings?.SchemaVersion == DmdClockSettings.CurrentSchemaVersion
                ? settings.Normalize()
                : DmdClockSettings.Default;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return DmdClockSettings.Default;
        }
    }

    public async Task SaveAtomicAsync(DmdClockSettings settings, string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("Settings path needs a directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await JsonSerializer.SerializeAsync(stream, settings.Normalize(), JsonOptions, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}

