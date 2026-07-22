using System.Text;

namespace DmdClock.App.Logging;

public sealed class AppFileLogger
{
    public const long MaxFileSizeBytes = 3L * 1024 * 1024;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public AppFileLogger(string path) => Path = System.IO.Path.GetFullPath(path);

    public string Path { get; }

    public async Task WriteAsync(DateTimeOffset timestampUtc, string message)
    {
        try
        {
            await _writeGate.WaitAsync().ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                var line = $"{timestampUtc:O} {message}{Environment.NewLine}";
                var lineBytes = Encoding.UTF8.GetBytes(line);
                if (lineBytes.LongLength > MaxFileSizeBytes)
                    lineBytes = lineBytes[^checked((int)MaxFileSizeBytes)..];

                if (File.Exists(Path) && new FileInfo(Path).Length + lineBytes.LongLength > MaxFileSizeBytes)
                    File.Move(Path, Path + ".previous", overwrite: true);

                using var stream = new FileStream(
                    Path, FileMode.Append, FileAccess.Write, FileShare.Read,
                    bufferSize: 4096, useAsync: true);
                await stream.WriteAsync(lineBytes).ConfigureAwait(false);
            }
            finally
            {
                _writeGate.Release();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Logging must never stop playback or library scanning.
        }
    }
}
