using System.Text.Json;

namespace DmdClock.Core.Library;

public sealed class SceneMetadataStore
{
    public const string DefaultFileName = "scene-metadata.json";
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    public async Task<SceneMetadataCatalog> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) return SceneMetadataCatalog.Empty;

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<SceneMetadataDocument>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false) ?? throw new JsonException("Scene metadata document is empty.");
        if (document.SchemaVersion != CurrentSchemaVersion)
            throw new JsonException($"Unsupported scene metadata schema {document.SchemaVersion}; expected {CurrentSchemaVersion}.");
        return new SceneMetadataCatalog(document.Prefixes, document.Files);
    }

    private sealed record SceneMetadataDocument(
        int SchemaVersion,
        IReadOnlyList<ScenePrefixMetadata>? Prefixes,
        IReadOnlyList<SceneFileMetadata>? Files);
}
