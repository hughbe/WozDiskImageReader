using System.Buffers.Binary;
using System.Diagnostics;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents a Track in a version 1 WOZ disk image.
/// </summary>
public readonly struct TrackV1
{
    /// <summary>
    /// The size of a track in bytes.
    /// </summary>
    public const int Size = 6656;

    /// <summary>
    /// Gets the track's bitstream, padded to 6646 bytes.
    /// </summary>
    public byte[] Bitstream { get; }

    /// <summary>
    /// Gets the actual byte count for the bitstream.
    /// </summary>
    public ushort BytesUsed { get; }

    /// <summary>
    /// Gets the number of bits in the bitstream.
    /// </summary>
    public ushort BitCount { get; }

    /// <summary>
    /// Gets the index of first bit after track splice (write hint). If no splice
    /// information is provided, then will be 0xFFFF.
    /// </summary>
    public ushort SplicePoint { get; }

    /// <summary>
    /// Gets the nibble value to use for splice (write hint).
    /// </summary>
    public byte SpliceNibble { get; }

    /// <summary>
    /// Gets the bit count of splice nibble (write hint).
    /// </summary>
    public byte SpliceBitCount { get; }

    /// <summary>
    /// Gets the reserved field (should be zero).
    /// </summary>
    public ushort Reserved { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackV1"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the Track from.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    public TrackV1(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference1/
        int offset = 0;

        // The bitstream data padded out to 6646 bytes
        Bitstream = new byte[6646];
        if (stream.Read(Bitstream) != Bitstream.Length)
        {
            throw new ArgumentException("Could not read Track from stream.", nameof(stream));
        }

        Span<byte> buffer = stackalloc byte[Size - Bitstream.Length];
        if (stream.Read(buffer) != buffer.Length)
        {
            throw new ArgumentException("Could not read Track from stream.", nameof(stream));
        }

        // The actual byte count for the bitstream.
        BytesUsed = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
        offset += 2;

        // The number of bits in the bitstream.
        BitCount = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
        offset += 2;

        // Index of first bit after track splice (write hint). If no splice information
        // is provided, then will be 0xFFFF.
        SplicePoint = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
        offset += 2;

        // Nibble value to use for splice (write hint).
        SpliceNibble = buffer[offset];
        offset += 1;

        // Bit count of splice nibble (write hint).
        SpliceBitCount = buffer[offset];
        offset += 1;

        // Reserved for future use.
        Reserved = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
        offset += 2;

        Debug.Assert(Bitstream.Length + offset == Size);
    }
}
