namespace DmdClock.Core.Scn;

public sealed record ScnInspectionResult(
    ScnCompatibilityStatus Status,
    ScnScene? Scene,
    IReadOnlyList<ScnDiagnostic> Diagnostics);
