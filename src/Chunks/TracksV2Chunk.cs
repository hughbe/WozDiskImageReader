namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the TracksChunk in a version 2 WOZ disk image.
/// </summary>
public readonly struct TracksV2Chunk
{
    /// <summary>
    /// The chunk ID for the TracksChunk.
    /// </summary>
    public static ReadOnlySpan<byte> ID => "TRKS"u8;

    private const int TrackCount = 160;

    /// <summary>
    /// Gets the tracks contained in this chunk.
    /// </summary>
    public TrackV2[] Tracks { get; }

    /// <summary>
    /// Gets the raw track data blocks.
    /// </summary>
    public byte[] TrackDataBlocks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TracksV2Chunk"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the TracksChunk from.</param>
    /// <param name="size">The size of the TracksChunk in bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    public TracksV2Chunk(Stream stream, int size)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Structure documented in https://applesaucefdc.com/woz/reference2/
        var offset = stream.Position;

        // Read all 160 track headers (8 bytes each = 1280 bytes) in a single I/O call.
        const int headerSize = TrackCount * TrackV2.Size;
        Span<byte> headerBuffer = stackalloc byte[headerSize];
        stream.ReadExactly(headerBuffer);

        var tracks = new TrackV2[TrackCount];
        for (int i = 0; i < TrackCount; i++)
        {
            tracks[i] = new TrackV2(headerBuffer.Slice(i * TrackV2.Size, TrackV2.Size));
        }

        Tracks = tracks;

        // Start of the actual track bits.
        var remainingLength = size - (stream.Position - offset);
        var trackDataBlocks = new byte[remainingLength];
        stream.ReadExactly(trackDataBlocks);
        TrackDataBlocks = trackDataBlocks;
    }
}
