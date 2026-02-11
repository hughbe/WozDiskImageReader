using System.Diagnostics;
using WozDiskImageReader.Utilities;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the Track Map chunk in a WOZ disk image.
/// </summary>
public readonly struct TrackMapChunk
{
    /// <summary>
    /// The chunk ID for the Track Map chunk.
    /// </summary>
    public static ReadOnlySpan<byte> ID => "TMAP"u8;

    /// <summary>
    /// The size of the Track Map chunk data in bytes.
    /// </summary>
    public const int Size = 160;

    /// <summary>
    /// Gets the track map entries.
    /// </summary>
    public ByteArray160 Entries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackMapChunk"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the Track Map chunk from.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    public TrackMapChunk(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Span<byte> buffer = stackalloc byte[Size];
        if (stream.Read(buffer) != Size)
        {
            throw new ArgumentException("Could not read Track Map chunk from stream.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference1/
        int offset = 0;

        Entries = new ByteArray160(buffer);
        offset += ByteArray160.Size;

        Debug.Assert(offset == buffer.Length, "Did not read entire Track Map chunk.");
    }
}
