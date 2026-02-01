using System.Diagnostics;
using WozDiskImageReader.Utilities;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the Flux chunk in a WOZ disk image.
/// </summary>
public readonly struct FluxChunk
{
    /// <summary>
    /// The chunk ID for the Flux chunk.
    /// </summary>
    public static ReadOnlySpan<byte> ID => "FLUX"u8;

    /// <summary>
    /// The size of the Flux chunk data in bytes.
    /// </summary>
    public const int Size = 160;

    /// <summary>
    /// Gets the Flux entries.
    /// </summary>
    public ByteArray160 Entries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FluxChunk"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the Flux chunk from.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    public FluxChunk(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        Span<byte> buffer = stackalloc byte[Size];
        if (stream.Read(buffer) != Size)
        {
            throw new ArgumentException("Could not read Flux chunk from stream.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference1/
        int offset = 0;

        Entries = new ByteArray160(buffer);
        offset += ByteArray160.Size;

        Debug.Assert(offset == buffer.Length, "Did not read entire Flux chunk.");
    }
}
