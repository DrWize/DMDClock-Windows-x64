using DmdClock.Core.Clock;

namespace DmdClock.Core.Tests.Clock;

public sealed class DotClkFontReaderTests
{
    [Fact]
    public void Read_RejectsTruncatedFont()
    {
        using var stream = new MemoryStream([1, 0, 1, (byte)'X']);

        var exception = Assert.Throws<InvalidDataException>(() => DotClkFontReader.Read(stream));

        Assert.Contains("truncated", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_RejectsUnsupportedVersion()
    {
        using var stream = new MemoryStream([2, 0]);

        var exception = Assert.Throws<InvalidDataException>(() => DotClkFontReader.Read(stream));

        Assert.Contains("version 2", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
