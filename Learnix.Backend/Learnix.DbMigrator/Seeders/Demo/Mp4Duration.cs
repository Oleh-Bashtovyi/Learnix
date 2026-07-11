using System.Buffers.Binary;

namespace Learnix.DbMigrator.Seeders;

/// <summary>
/// Reads a duration straight out of an MP4 header, so a seeded video lesson carries the same length
/// an instructor-uploaded one gets from the browser. No decoder and no NuGet package: the number
/// lives in the <c>mvhd</c> box, a fixed-layout header a few dozen bytes into the file.
/// </summary>
internal static class Mp4Duration
{
    /// <summary>Seconds, rounded up, or null when the file is not an MP4 we can read.</summary>
    public static int? TryRead(Stream stream)
    {
        try
        {
            return FindMvhd(stream, stream.Length);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Walks the sibling boxes at the current level. Boxes are <c>[size:4][type:4][payload]</c>;
    /// only <c>moov</c> is worth descending into, and inside it sits <c>mvhd</c>.
    /// </summary>
    // S3776: a binary format walker. The branches are the MP4 box layout itself — 32-bit vs 64-bit
    // sizes, version 0 vs 1 of mvhd, a truncated file — and splitting them across methods would hide
    // the layout rather than explain it.
#pragma warning disable S3776
    private static int? FindMvhd(Stream stream, long end)
    {
        var header = new byte[8];

        while (stream.Position + 8 <= end)
        {
            var boxStart = stream.Position;

            if (!ReadExactly(stream, header))
                return null;

            long size = BinaryPrimitives.ReadUInt32BigEndian(header);
            var type = System.Text.Encoding.ASCII.GetString(header, 4, 4);

            // size 1 means the real size is a 64-bit value right after the type; 0 means "to EOF".
            if (size == 1)
            {
                var largeSize = new byte[8];
                if (!ReadExactly(stream, largeSize))
                    return null;
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(largeSize);
            }
            else if (size == 0)
            {
                size = end - boxStart;
            }

            if (size < 8)
                return null;

            if (type == "mvhd")
                return ReadMvhd(stream);

            if (type == "moov")
            {
                var found = FindMvhd(stream, boxStart + size);
                if (found is not null)
                    return found;
            }

            stream.Position = boxStart + size;
        }

        return null;
    }
#pragma warning restore S3776

    private static int? ReadMvhd(Stream stream)
    {
        var version = stream.ReadByte();
        if (version < 0)
            return null;

        stream.Position += 3; // flags

        // Layout after version+flags. Version 0 stores 32-bit times, version 1 stores 64-bit ones:
        //   v0: creation(4) modification(4) timescale(4) duration(4)
        //   v1: creation(8) modification(8) timescale(4) duration(8)
        // Timescale is ticks per second, duration is in those ticks.
        var payload = new byte[version == 1 ? 28 : 16];
        if (!ReadExactly(stream, payload))
            return null;

        var timescale = version == 1
            ? BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(16, 4))
            : BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(8, 4));

        var duration = version == 1
            ? BinaryPrimitives.ReadUInt64BigEndian(payload.AsSpan(20, 8))
            : BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(12, 4));

        return timescale == 0 ? null : (int)Math.Ceiling(duration / (double)timescale);
    }

    private static bool ReadExactly(Stream stream, byte[] buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = stream.Read(buffer, read, buffer.Length - read);
            if (n == 0)
                return false;
            read += n;
        }

        return true;
    }
}
