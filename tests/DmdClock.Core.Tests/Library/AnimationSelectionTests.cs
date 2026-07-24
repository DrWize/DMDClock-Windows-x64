using DmdClock.Core.Library;

namespace DmdClock.Core.Tests.Library;

public sealed class AnimationSelectionTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"dmdclock-selection-{Guid.NewGuid():N}");

    [Fact]
    public async Task Store_RoundTripsNormalizedSelectionAtomically()
    {
        var path = Path.Combine(_directory, "library-selections.json");
        var document = AnimationSelectionDocument.Empty with
        {
            LibraryRoot = _directory,
            Columns = 99,
            Rows = 0,
            EnabledGames = [" Attack from Mars ", "attack from mars"],
            Scenes =
            [
                new("ID1", @"folder\scene.scn", "HASH", AnimationSelectionState.Allowed)
            ]
        };
        var store = new AnimationSelectionStore();

        await store.SaveAtomicAsync(document, path);
        var loaded = await store.LoadAsync(path);

        Assert.Equal(20, loaded.Columns);
        Assert.Equal(1, loaded.Rows);
        Assert.Equal(["Attack from Mars"], loaded.EnabledGames);
        Assert.Equal("folder/scene.scn", Assert.Single(loaded.Scenes).LastRelativePath);
        Assert.Empty(Directory.EnumerateFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Resolver_RequiresEnabledGameAndExplicitlyAllowedScene()
    {
        var item = Catalog("ID1", "afm01.scn", "HASH", "Attack from Mars");
        var unreviewed = AnimationSelectionDocument.Empty;
        var allowed = AnimationSelectionResolver.SetSceneState(
            AnimationSelectionResolver.SetGameEnabled(
                unreviewed, "Attack from Mars", enabled: true),
            item,
            AnimationSelectionState.Allowed);

        Assert.Empty(AnimationSelectionResolver.ResolvePlayable([item], unreviewed));
        Assert.Equal(
            ["afm01.scn"],
            AnimationSelectionResolver.ResolvePlayable([item], allowed)
                .Select(static scene => scene.RelativePath));
        Assert.Empty(AnimationSelectionResolver.ResolvePlayable(
            [item],
            AnimationSelectionResolver.SetGameEnabled(
                allowed, "Attack from Mars", enabled: false)));
    }

    [Fact]
    public void Resolver_ReconcilesUniqueHashThenPath()
    {
        var original = Catalog("OLD", "old.scn", "SAME", "Game");
        var document = AnimationSelectionResolver.SetSceneState(
            AnimationSelectionDocument.Empty,
            original,
            AnimationSelectionState.Disallowed);

        Assert.Equal(
            AnimationSelectionState.Disallowed,
            AnimationSelectionResolver.ResolveState(
                Catalog("NEW", "moved.scn", "SAME", "Game"), document));
        Assert.Equal(
            AnimationSelectionState.Disallowed,
            AnimationSelectionResolver.ResolveState(
                Catalog("NEW", "old.scn", "CHANGED", "Game"), document));
    }

    [Fact]
    public async Task Store_InvalidJsonFallsBackToEmptySelection()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "library-selections.json");
        await File.WriteAllTextAsync(path, "{broken");

        var loaded = await new AnimationSelectionStore().LoadAsync(path);

        Assert.Equal(AnimationSelectionDocument.Empty, loaded);
    }

    private static AnimationCatalogItem Catalog(
        string id,
        string path,
        string hash,
        string game) =>
        new(
            new AnimationLibraryItem(
                id, path, 1, DateTimeOffset.UnixEpoch, hash, 1, 100, null, []),
            new ResolvedSceneMetadata(
                path, Path.GetFileName(path), game, null, game, null, null, null, null, null, null));

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
