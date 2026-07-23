using DmdClock.Core.Scn;

namespace DmdClock.Core.Tests.Scn;

public sealed class ScnCompatibilityInspectorTests
{
    [Fact]
    public void Inspect_AcceptsValidScene()
    {
        using var stream = TestScnFile.Create();

        var result = ScnCompatibilityInspector.Inspect(stream);

        Assert.Equal(ScnCompatibilityStatus.Accepted, result.Status);
        Assert.NotNull(result.Scene);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Inspect_WarnsWhenRegularFrameDelayIsZero()
    {
        using var stream = TestScnFile.Create(frameCount: 2, frameDelayMs: 0);

        var result = ScnCompatibilityInspector.Inspect(stream);

        Assert.Equal(ScnCompatibilityStatus.Warned, result.Status);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal("invalid-frame-delay", warning.Code);
        Assert.Contains("100 ms", warning.Message);
    }

    [Fact]
    public void Inspect_AcceptsUnusedZeroRegularDelay()
    {
        using var stream = TestScnFile.Create(frameCount: 1, frameDelayMs: 0);

        var result = ScnCompatibilityInspector.Inspect(stream);

        Assert.Equal(ScnCompatibilityStatus.Accepted, result.Status);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Inspect_RejectsUnknownVersionWithSpecificDiagnostic()
    {
        using var stream = TestScnFile.Create();
        stream.GetBuffer()[0] = 2;

        var result = ScnCompatibilityInspector.Inspect(stream);

        Assert.Equal(ScnCompatibilityStatus.Rejected, result.Status);
        Assert.Null(result.Scene);
        Assert.Equal("unsupported-version", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void Inspect_RejectsDamagedAndEmptyScenes()
    {
        using var valid = TestScnFile.Create();
        using var truncated = new MemoryStream(valid.ToArray()[..^1]);
        using var empty = TestScnFile.Create(frameCount: 0);

        var damagedResult = ScnCompatibilityInspector.Inspect(truncated);
        var emptyResult = ScnCompatibilityInspector.Inspect(empty);

        Assert.Equal(ScnCompatibilityStatus.Rejected, damagedResult.Status);
        Assert.Equal("damaged-file", Assert.Single(damagedResult.Diagnostics).Code);
        Assert.Equal(ScnCompatibilityStatus.Rejected, emptyResult.Status);
        Assert.Equal("no-frames", Assert.Single(emptyResult.Diagnostics).Code);
    }
}
