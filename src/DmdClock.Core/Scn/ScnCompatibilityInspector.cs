namespace DmdClock.Core.Scn;

public static class ScnCompatibilityInspector
{
    public static ScnInspectionResult Inspect(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            using var stream = File.OpenRead(path);
            return Inspect(stream);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Rejected("access-denied", exception.Message);
        }
        catch (IOException exception)
        {
            return Rejected("io-error", exception.Message);
        }
    }

    public static ScnInspectionResult Inspect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            var scene = ScnReader.Read(stream);
            if (scene.Frames.Count == 0)
                return Rejected("no-frames", "SCN does not contain any animation frames.");

            var warnings = new List<ScnDiagnostic>();
            if (scene.Storyboards.Count == 0)
            {
                warnings.Add(new ScnDiagnostic(
                    "missing-storyboard",
                    "SCN has no storyboard; playback uses the default 100 ms frame delay."));
            }
            else
            {
                for (var index = 0; index < scene.Storyboards.Count; index++)
                {
                    var storyboard = scene.Storyboards[index];
                    var firstFrameIsConsumed =
                        storyboard.FirstFrameDelayMs > 0 && !storyboard.BlankFirstFrame;
                    var regularFrameCount = scene.Frames.Count - (firstFrameIsConsumed ? 1 : 0);
                    if (storyboard.FrameDelayMs == 0 && regularFrameCount > 0)
                    {
                        warnings.Add(new ScnDiagnostic(
                            "invalid-frame-delay",
                            $"Storyboard {index} has a zero regular frame delay; playback uses 100 ms."));
                    }
                }
            }

            if (scene.Storyboards.Count > 1)
            {
                warnings.Add(new ScnDiagnostic(
                    "multiple-storyboards",
                    $"SCN contains {scene.Storyboards.Count} storyboards; playback currently uses the first."));
            }

            return new ScnInspectionResult(
                warnings.Count == 0 ? ScnCompatibilityStatus.Accepted : ScnCompatibilityStatus.Warned,
                scene,
                warnings);
        }
        catch (ScnFormatException exception)
        {
            var code = exception.Message.StartsWith(
                "Unsupported SCN version",
                StringComparison.OrdinalIgnoreCase)
                ? "unsupported-version"
                : "damaged-file";
            return Rejected(code, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Rejected("access-denied", exception.Message);
        }
        catch (IOException exception)
        {
            return Rejected("io-error", exception.Message);
        }
    }

    private static ScnInspectionResult Rejected(string code, string message) =>
        new(
            ScnCompatibilityStatus.Rejected,
            null,
            [new ScnDiagnostic(code, message)]);
}
