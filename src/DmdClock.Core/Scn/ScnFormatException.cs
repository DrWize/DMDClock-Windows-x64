namespace DmdClock.Core.Scn;

public sealed class ScnFormatException : IOException
{
    public ScnFormatException(string message) : base(message) { }

    public ScnFormatException(string message, Exception innerException) : base(message, innerException) { }
}
