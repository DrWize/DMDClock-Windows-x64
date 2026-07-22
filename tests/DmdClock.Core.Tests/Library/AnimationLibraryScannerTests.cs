using DmdClock.Core.Library;

namespace DmdClock.Core.Tests.Library;

public sealed class AnimationLibraryScannerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dmdclock-tests-{Guid.NewGuid():N}");

    public AnimationLibraryScannerTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Scan_ReusesIdAfterFileMoveAndReportsBrokenFiles()
    {
        var firstPath = Path.Combine(_directory, "scene2.scn");
        await using (var source = TestScnFile.Create(frameCount: 2))
        await using (var destination = File.Create(firstPath))
            await source.CopyToAsync(destination);
        await File.WriteAllBytesAsync(Path.Combine(_directory, "broken.scn"), [1, 2, 3]);
        var scanner = new AnimationLibraryScanner();

        var first = await scanner.ScanAsync(_directory);
        var valid = Assert.Single(first.Items, static item => item.IsValid);
        Assert.Single(first.Items, static item => !item.IsValid);

        var movedPath = Path.Combine(_directory, "scene10.scn");
        File.Move(firstPath, movedPath);
        var second = await scanner.ScanAsync(_directory, first);

        Assert.Equal(valid.Id, Assert.Single(second.Items, static item => item.IsValid).Id);
        Assert.Equal(["broken.scn", "scene10.scn"], second.Items.Select(static item => item.RelativePath));
    }

    [Fact]
    public async Task Store_RoundTripsIndexAtomically()
    {
        var path = Path.Combine(_directory, "index.json");
        var expected = new AnimationLibraryIndex(1, _directory, DateTimeOffset.UnixEpoch, []);
        var store = new AnimationLibraryStore();

        await store.SaveAtomicAsync(expected, path);
        var actual = await store.LoadAsync(path);

        Assert.NotNull(actual);
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.RootPath, actual.RootPath);
        Assert.Equal(expected.ScannedAtUtc, actual.ScannedAtUtc);
        Assert.Empty(actual.Items);
        Assert.Empty(Directory.EnumerateFiles(_directory, "*.tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
