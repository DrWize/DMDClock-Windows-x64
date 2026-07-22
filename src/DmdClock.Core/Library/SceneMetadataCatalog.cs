namespace DmdClock.Core.Library;

public sealed record ScenePrefixMetadata(
    string Prefix,
    string Game,
    string? Manufacturer = null,
    int? Year = null,
    string? DateManufactured = null,
    int? Players = null,
    string? MachineType = null,
    string? Theme = null);

public sealed record SceneFileMetadata(
    string Path,
    string? Title = null,
    string? Game = null,
    string? Manufacturer = null,
    int? Year = null,
    string? DateManufactured = null,
    int? Players = null,
    string? MachineType = null,
    string? Theme = null);

public sealed record ResolvedSceneMetadata(
    string RelativePath,
    string FileName,
    string DisplayName,
    string? Title,
    string? Game,
    string? Manufacturer,
    int? Year,
    string? DateManufactured,
    int? Players,
    string? MachineType,
    string? Theme);

public sealed class SceneMetadataCatalog
{
    private readonly IReadOnlyList<ScenePrefixMetadata> _prefixes;
    private readonly IReadOnlyDictionary<string, SceneFileMetadata> _files;

    public SceneMetadataCatalog(
        IEnumerable<ScenePrefixMetadata>? prefixes = null,
        IEnumerable<SceneFileMetadata>? files = null)
    {
        var prefixArray = (prefixes ?? []).ToArray();
        var fileArray = (files ?? []).ToArray();
        ValidatePrefixes(prefixArray);
        ValidateFiles(fileArray);
        _prefixes = prefixArray
            .OrderByDescending(static item => item.Prefix.Length)
            .ThenBy(static item => item.Prefix, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _files = fileArray.ToDictionary(
            static item => NormalizePath(item.Path),
            StringComparer.OrdinalIgnoreCase);
    }

    public static SceneMetadataCatalog Empty { get; } = new();

    public ResolvedSceneMetadata Resolve(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var normalizedPath = NormalizePath(relativePath);
        var fileName = Path.GetFileName(normalizedPath);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        _files.TryGetValue(normalizedPath, out var file);
        var prefix = _prefixes.FirstOrDefault(item => baseName.StartsWith(item.Prefix, StringComparison.OrdinalIgnoreCase));

        var title = NullIfWhiteSpace(file?.Title);
        var game = NullIfWhiteSpace(file?.Game) ?? prefix?.Game;
        var manufacturer = NullIfWhiteSpace(file?.Manufacturer) ?? prefix?.Manufacturer;
        var year = file?.Year ?? prefix?.Year;
        var dateManufactured = NullIfWhiteSpace(file?.DateManufactured) ?? prefix?.DateManufactured;
        var players = file?.Players ?? prefix?.Players;
        var machineType = NullIfWhiteSpace(file?.MachineType) ?? prefix?.MachineType;
        var theme = NullIfWhiteSpace(file?.Theme) ?? prefix?.Theme;
        var displayName = title ?? (game is null ? baseName : $"{game} — {baseName}");
        return new ResolvedSceneMetadata(
            normalizedPath, fileName, displayName, title, game, manufacturer, year,
            dateManufactured, players, machineType, theme);
    }

    private static void ValidatePrefixes(IReadOnlyList<ScenePrefixMetadata> prefixes)
    {
        foreach (var item in prefixes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Prefix);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Game);
            ValidateYear(item.Year);
            ValidatePlayers(item.Players);
        }
        if (prefixes.GroupBy(static item => item.Prefix, StringComparer.OrdinalIgnoreCase).Any(static group => group.Count() > 1))
            throw new ArgumentException("Scene metadata contains duplicate prefixes.", nameof(prefixes));
    }

    private static void ValidateFiles(IReadOnlyList<SceneFileMetadata> files)
    {
        foreach (var item in files)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Path);
            ValidateYear(item.Year);
            ValidatePlayers(item.Players);
        }
        if (files.GroupBy(static item => NormalizePath(item.Path), StringComparer.OrdinalIgnoreCase).Any(static group => group.Count() > 1))
            throw new ArgumentException("Scene metadata contains duplicate file paths.", nameof(files));
    }

    private static void ValidateYear(int? year)
    {
        if (year is < 1930 or > 2200)
            throw new ArgumentOutOfRangeException(nameof(year), "Metadata year must be between 1930 and 2200.");
    }

    private static void ValidatePlayers(int? players)
    {
        if (players is < 1 or > 16)
            throw new ArgumentOutOfRangeException(nameof(players), "Metadata players must be between 1 and 16.");
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
