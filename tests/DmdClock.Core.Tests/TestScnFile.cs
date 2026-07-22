namespace DmdClock.Core.Tests;

internal static class TestScnFile
{
    public static MemoryStream Create(bool includeMask = false, ushort frameCount = 1, ushort frameDelayMs = 125)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)1);
            writer.Write(frameCount);
            writer.Write((ushort)1);
            writer.Write((ushort)10);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(frameDelayMs);
            writer.Write((ushort)1);
            writer.Write((ushort)20);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((byte)1);
            writer.Write((byte)7);
            writer.Write((byte)8);
            writer.Write(new byte[17]);

            for (var frame = 0; frame < frameCount; frame++)
            {
                writer.Write((ushort)128);
                writer.Write((ushort)32);
                writer.Write((ushort)4);
                writer.Write((ushort)(includeMask ? 1 : 0));
                writer.Write((byte)(0xa1 + frame));
                writer.Write(new byte[(128 * 32 / 2) - 1]);
                if (includeMask)
                {
                    writer.Write((byte)0x81);
                    writer.Write(new byte[(128 * 32 / 8) - 1]);
                }
            }
        }

        stream.Position = 0;
        return stream;
    }
}

