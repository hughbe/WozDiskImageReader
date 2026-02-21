using System.Buffers.Binary;
using System.Diagnostics;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents a write command (WCMD) in a WriteTrack structure.
/// </summary>
public readonly struct WriteCommand
{
    /// <summary>
    /// The size of a WriteCommand in bytes.
    /// </summary>
    public const int Size = 12;

    /// <summary>
    /// Gets the starting bit index where to begin writing in the bitstream.
    /// </summary>
    public uint StartingBitIndex { get; }

    /// <summary>
    /// Gets the number of bits to write.
    /// </summary>
    public uint BitCount { get; }

    /// <summary>
    /// Gets the nibble value for pre-data padding (leader nibbles).
    /// Typically 0xFF for DOS 3.3/ProDOS. 0x00 if unused.
    /// </summary>
    public byte LeaderNibble { get; }

    /// <summary>
    /// Gets the number of bits per leader nibble.
    /// Commonly 10 for DOS 3.3/ProDOS.
    /// </summary>
    public byte LeaderBitCount { get; }

    /// <summary>
    /// Gets the quantity of leader nibbles to write.
    /// Provides clean gap spacing before data.
    /// </summary>
    public byte LeaderCount { get; }

    /// <summary>
    /// Gets the reserved byte (must be zero).
    /// </summary>
    public byte Reserved { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteCommand"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the WriteCommand from.</param>
    /// <exception cref="ArgumentException">Thrown when the command cannot be read or contains invalid data.</exception>
    public WriteCommand(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Span<byte> buffer = stackalloc byte[Size];
        stream.ReadExactly(buffer);

        // Structure documented in https://applesaucefdc.com/woz/reference2/
        int offset = 0;

        // The index of the first bit of this write.
        StartingBitIndex = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));
        offset += 4;

        // The number of bits to write.
        BitCount = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));
        offset += 4;

        // If this write requires leader nibbles, then this is the value for it.
        // Typical value for this is 0xFF. If no leader is needed, then set value
        // to 0x00.
        LeaderNibble = buffer[offset];
        offset += 1;

        // The number of bits that leader nibbles have.
        // Typical value for DOS 3.3 and ProDOS would be 10.
        LeaderBitCount = buffer[offset];
        offset += 1;

        // The number of Leader Nibbles that should be written before the bit data.
        LeaderCount = buffer[offset];
        offset += 1;

        // Reserved and currently used to pad each WCMD to 4 byte boundaries.
        // Must be zero.
        Reserved = buffer[offset];
        offset += 1;

        Debug.Assert(offset == buffer.Length, "Did not read entire WriteCommand.");
    }
}
