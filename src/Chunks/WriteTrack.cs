using System.Buffers.Binary;
using System.Diagnostics;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents a WriteTrack in the WRITES chunk of a WOZ disk image.
/// </summary>
public readonly struct WriteTrack
{
    /// <summary>
    /// The minimum size of a WriteTrack in bytes.
    /// </summary>
    public const int MinSize = 8;

    /// <summary>
    /// Gets the track number at which to write this data. For 5.25 disks, this
    /// value is in half-phases or quarter tracks (0 = 0.00, 4 = 1.00, 5 = 1.25).
    /// For 3.5 disks, this value indicates track number as well as side.
    /// The formula ((track &lt;&lt; 1) + side) can be used (0 = Track 0, Side 0,
    /// 1 = Track 0, Side 1, 2 = Track 1, Side 0).
    /// </summary>
    public byte TrackNumber { get; }

    /// <summary>
    /// Gets the number of commands in the write array.
    /// </summary>
    public byte CommandCount { get; }

    /// <summary>
    /// Gets the bit field with a 1 indicating to perform the action:
    /// 0x01 = Wipe the track before writing.
    /// </summary>
    public WriteTrackFunctions AdditionalWriteFunctions { get; }

    /// <summary>
    /// Reserved and must be zero.
    /// </summary>
    public byte Reserved { get; }

    /// <summary>
    /// Gets the checksum of the entire used BITS data of this TRK.
    /// The proper data length can be calculated by (TRK.bitCount + 7) / 8.
    /// Used to determine if the BITS has changed since the WRIT chunk was written.
    /// </summary>
    public uint Checksum { get; }

    /// <summary>
    /// Gets the write commands for this track.
    /// </summary>
    public WriteCommand[] Commands { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteTrack"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the WriteTrack from.</param>
    /// <exception cref="ArgumentException">Thrown when the WriteTrack cannot be read or contains invalid data.</exception>
    public WriteTrack(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Span<byte> buffer = stackalloc byte[MinSize];
        if (stream.Read(buffer) != MinSize)
        {
            throw new ArgumentException("Could not read entire WriteTrack from stream.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference2/
        int offset = 0;

        // The track number at which to write this data.
        // For 5.25 disks, this value is in half-phases or quarter tracks
        // (0 = 0.00, 4 = 1.00, 5 = 1.25). For 3.5 disks, this value indicates
        // track number as well as side. The formula ((track << 1) + side) can
        // be used (0 = Track 0, Side 0, 1 = Track 0, Side 1, 2 = Track 1, Side 0).
        TrackNumber = buffer[offset];
        offset += 1;

        // The number of commands in the write array.
        CommandCount = buffer[offset];
        offset += 1;

        // Bit field with a 1 indicating to perform the action:
        // 0x01 = Wipe the track before writing.
        AdditionalWriteFunctions = (WriteTrackFunctions)buffer[offset];
        offset += 1;

        // Reserved and must be zero.
        Reserved = buffer[offset];
        offset += 1;
        
        // Gets Checksum of the entire used BITS data of this TRK. The proper data
        // length can be calculated by (TRK.bitCount + 7) / 8. Used to determine if
        // the BITS has changed since the WRIT chunk was written.
        Checksum = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));
        offset += 4;

        var commands = new WriteCommand[CommandCount];
        for (int i = 0; i < CommandCount; i++)
        {
            commands[i] = new WriteCommand(stream);
        }

        Commands = commands;

        Debug.Assert(offset == MinSize, "Did not read entire WriteTrack header.");
    }
}
