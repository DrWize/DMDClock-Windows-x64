using DmdClock.App.Logging;

namespace DmdClock.Core.Tests.Logging;

public sealed class AppFileLoggerTests
{
    [Fact]
    public async Task WriteAsync_RotatesBeforeActiveLogExceedsThreeMegabytes()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dmdclock-log-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "dmdclock.log");
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllBytesAsync(path, new byte[checked((int)AppFileLogger.MaxFileSizeBytes) - 64]);
            var logger = new AppFileLogger(path);

            await logger.WriteAsync(DateTimeOffset.UtcNow, new string('X', 256));

            Assert.True(File.Exists(path + ".previous"));
            Assert.InRange(new FileInfo(path).Length, 1, AppFileLogger.MaxFileSizeBytes);
            Assert.Equal(AppFileLogger.MaxFileSizeBytes - 64, new FileInfo(path + ".previous").Length);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
