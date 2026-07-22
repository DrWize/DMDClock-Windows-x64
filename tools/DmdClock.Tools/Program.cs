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
    Console.WriteLine($"Valid: {index.Items.Count(static item => item.IsValid)}");
    Console.WriteLine($"Failures: {index.Items.Count(static item => !item.IsValid)}");
    Console.WriteLine($"Frames: {index.Items.Sum(static item => (long)item.FrameCount)}");
    foreach (var item in index.Items.Where(static item => !item.IsValid).Take(20))
        Console.Error.WriteLine($"{item.RelativePath}: {item.Error}");
    return index.Items.Any(static item => !item.IsValid) ? 1 : 0;
}

var files = Directory.EnumerateFiles(root, "*.scn", SearchOption.AllDirectories)
    .Order(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var failures = new List<(string Path, string Error)>();
long frameCount = 0;
long maskedFrameCount = 0;

foreach (var file in files)
{
    try
    {
        var scene = ScnReader.Read(file);
        frameCount += scene.Frames.Count;
        maskedFrameCount += scene.Frames.Count(static frame => frame.Mask is not null);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        failures.Add((file, exception.Message));
    }
}

Console.WriteLine($"Files: {files.Length}");
Console.WriteLine($"Frames: {frameCount}");
Console.WriteLine($"Masked frames: {maskedFrameCount}");
Console.WriteLine($"Failures: {failures.Count}");

foreach (var failure in failures.Take(20))
    Console.Error.WriteLine($"{failure.Path}: {failure.Error}");

return failures.Count == 0 ? 0 : 1;
