using DmdClock.Core.Library;
using DmdClock.Core.Scn;

if (args.Length != 2 ||
    (!string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase) &&
     !string.Equals(args[0], "index", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine("Usage: DmdClock.Tools <scan|index> <directory>");
    return 2;
}

var root = Path.GetFullPath(args[1]);
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Directory not found: {root}");
    return 2;
}

if (string.Equals(args[0], "index", StringComparison.OrdinalIgnoreCase))
{
    var index = await new AnimationLibraryScanner().ScanAsync(root);
    Console.WriteLine($"Files: {index.Items.Count}");
    Console.WriteLine($"Accepted: {index.Items.Count(static item => item.IsValid && (item.Warnings?.Count ?? 0) == 0)}");
    Console.WriteLine($"Warned: {index.Items.Count(static item => item.IsValid && (item.Warnings?.Count ?? 0) > 0)}");
    Console.WriteLine($"Rejected: {index.Items.Count(static item => !item.IsValid)}");
    Console.WriteLine($"Frames: {index.Items.Sum(static item => (long)item.FrameCount)}");
    foreach (var item in index.Items.Where(static item => (item.Warnings?.Count ?? 0) > 0))
    foreach (var warning in item.Warnings!)
        Console.WriteLine($"WARN {item.RelativePath}: [{warning.Code}] {warning.Message}");
    foreach (var item in index.Items.Where(static item => !item.IsValid))
        Console.WriteLine($"REJECT {item.RelativePath}: {item.Error}");
    return index.Items.Any(static item => !item.IsValid) ? 1 : 0;
}

var files = Directory.EnumerateFiles(root, "*.scn", SearchOption.AllDirectories)
    .Order(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var diagnostics = new List<(ScnCompatibilityStatus Status, string Path, ScnDiagnostic Diagnostic)>();
long frameCount = 0;
long maskedFrameCount = 0;
var acceptedCount = 0;
var warnedCount = 0;
var rejectedCount = 0;

foreach (var file in files)
{
    var inspection = ScnCompatibilityInspector.Inspect(file);
    switch (inspection.Status)
    {
        case ScnCompatibilityStatus.Accepted:
            acceptedCount++;
            break;
        case ScnCompatibilityStatus.Warned:
            warnedCount++;
            break;
        case ScnCompatibilityStatus.Rejected:
            rejectedCount++;
            break;
    }

    if (inspection.Scene is not null)
    {
        frameCount += inspection.Scene.Frames.Count;
        maskedFrameCount += inspection.Scene.Frames.Count(static frame => frame.Mask is not null);
    }

    var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
    diagnostics.AddRange(inspection.Diagnostics.Select(diagnostic =>
        (inspection.Status, relativePath, diagnostic)));
}

Console.WriteLine($"Files: {files.Length}");
Console.WriteLine($"Accepted: {acceptedCount}");
Console.WriteLine($"Warned: {warnedCount}");
Console.WriteLine($"Rejected: {rejectedCount}");
Console.WriteLine($"Frames: {frameCount}");
Console.WriteLine($"Masked frames: {maskedFrameCount}");
Console.WriteLine();
Console.WriteLine("Diagnostics:");
if (diagnostics.Count == 0)
{
    Console.WriteLine("None");
}
else
{
    foreach (var item in diagnostics)
    {
        var label = item.Status == ScnCompatibilityStatus.Rejected ? "REJECT" : "WARN";
        Console.WriteLine($"{label} {item.Path}: [{item.Diagnostic.Code}] {item.Diagnostic.Message}");
    }
}

return rejectedCount == 0 ? 0 : 1;
