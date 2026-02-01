using System.Buffers.Binary;
using System.Diagnostics;
using WozDiskImageReader.Utilities;

namespace WozDiskImageReader;

/// <summary>
/// Represents a chunk within a WOZ disk image.
/// </summary>
public readonly struct WozDiskImageChunk
{
    /// <summary>
    /// Minimum size of a WOZ chunk (ID + Size fields).
    /// </summary>
    public const int MinSize = 8;

    /// <summary>
    /// Gets the offset of the chunk within the WOZ disk image stream.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the chunk ID.
    /// </summary>
    public WozDiskImageChunkID ID { get; }

    /// <summary>
    /// Gets the size of the chunk data in bytes.
    /// </summary>
    public uint Size { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WozDiskImageChunk"/> struct.
    /// </summary>
    /// <param name="stream">The stream representing the WOZ disk image.</param>
    /// <exception cref="ArgumentException">Thrown if the stream is not seekable or readable.</exception>
    /// <exception cref="InvalidDataException">Thrown if the stream is too short to contain a valid WOZ chunk.</exception>
    public WozDiskImageChunk(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        Offset = stream.Position;

        Span<byte> buffer = stackalloc byte[MinSize];
        if (stream.Read(buffer) != MinSize)
        {
            throw new ArgumentException("Stream is too short to contain a valid WOZ chunk.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference1/
        int offset = 0;

        // 4 ASCII characters that make up the ID of the chunk
        ID = new WozDiskImageChunkID(buffer.Slice(offset, WozDiskImageChunkID.Size));
        offset += WozDiskImageChunkID.Size;

        // The size of the chunk data in bytes.
        Size = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));
        offset += 4;

        Debug.Assert(offset == MinSize && offset <= buffer.Length, "Did not consume entire WOZ chunk header.");
    }
}
