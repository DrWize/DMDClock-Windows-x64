namespace DmdClock.Core.Library;

public static class AnimationSelectionResolver
{
    public static IReadOnlyList<AnimationCatalogItem> BuildCatalog(
        IEnumerable<AnimationLibraryItem> items,
        SceneMetadataCatalog metadata) =>
        items.Select(item => new AnimationCatalogItem(
                item,
                metadata.Resolve(item.RelativePath)))
            .ToArray();

    public static IReadOnlyList<AnimationLibraryItem> ResolvePlayable(
        IEnumerable<AnimationCatalogItem> catalog,
        AnimationSelectionDocument document)
    {
        var normalized = document.Normalize();
        var enabledGames = normalized.EnabledGames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return catalog
            .Where(item =>
                item.LibraryItem.IsValid &&
                enabledGames.Contains(item.Game) &&
                ResolveState(item, normalized) == AnimationSelectionState.Allowed)
            .Select(static item => item.LibraryItem)
            .ToArray();
    }

    public static AnimationSelectionState ResolveState(
        AnimationCatalogItem item,
        AnimationSelectionDocument document)
    {
        var entries = document.Scenes ?? [];
        var byId = entries.LastOrDefault(entry =>
            string.Equals(entry.Id, item.LibraryItem.Id, StringComparison.OrdinalIgnoreCase));
        if (byId is not null) return byId.State;

        if (!string.IsNullOrWhiteSpace(item.LibraryItem.Sha256))
        {
            var byHash = entries.Where(entry =>
                    !string.IsNullOrWhiteSpace(entry.Sha256) &&
                    string.Equals(
                        entry.Sha256, item.LibraryItem.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (byHash.Length == 1) return byHash[0].State;
        }

        var normalizedPath = AnimationSelectionDocument.NormalizePath(
            item.LibraryItem.RelativePath);
        return entries.LastOrDefault(entry =>
                string.Equals(
                    AnimationSelectionDocument.NormalizePath(entry.LastRelativePath),
                    normalizedPath,
                    StringComparison.OrdinalIgnoreCase))
            ?.State ?? AnimationSelectionState.Unreviewed;
    }

    public static AnimationSelectionDocument SetSceneState(
        AnimationSelectionDocument document,
        AnimationCatalogItem item,
        AnimationSelectionState state)
    {
        var entries = document.Scenes
            .Where(entry =>
                !string.Equals(
                    entry.Id, item.LibraryItem.Id, StringComparison.OrdinalIgnoreCase))
            .Append(new AnimationSelectionEntry(
                item.LibraryItem.Id,
                item.LibraryItem.RelativePath,
                item.LibraryItem.Sha256,
                state))
            .ToArray();
        return (document with { Scenes = entries }).Normalize();
    }

    public static AnimationSelectionDocument SetGameEnabled(
        AnimationSelectionDocument document,
        string game,
        bool enabled)
    {
        var games = document.EnabledGames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (enabled) games.Add(game);
        else games.Remove(game);
        return (document with { EnabledGames = games.ToArray() }).Normalize();
    }
}
