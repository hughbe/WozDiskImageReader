using System.Buffers.Binary;
using System.Diagnostics;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents a track in a version 2 WOZ disk image.
/// </summary>
public readonly struct TrackV2
{
    /// <summary>
    /// The size of the TrackV2 structure in bytes.
    /// </summary>
    public const int Size = 8;

    /// <summary>
    /// Gets the starting block of the track.
    /// </summary>
    public ushort StartBlock { get; }

    /// <summary>
    /// Gets the number of blocks in the track.
    /// </summary>
    public ushort BlockCount { get; }

    /// <summary>
    /// Gets the number of bits in the track.
    /// </summary>
    public uint BitCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackV2"/> struct by reading from the provided data.
    /// </summary>
    /// <param name="data">The data to read the TrackV2 from.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the expected size.</exception>
    public TrackV2(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data length for TrackV2 must be exactly {Size} bytes.", nameof(data));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference2/
        int offset = 0;

        StartBlock = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        BlockCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        BitCount = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        Debug.Assert(offset == data.Length, "Did not consume all data for TrackV2 header.");
    }
}
