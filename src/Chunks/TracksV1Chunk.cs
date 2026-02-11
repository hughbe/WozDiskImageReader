namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the TracksChunk in a version 1 WOZ disk image.
/// </summary>
public readonly struct TracksV1Chunk
{
    /// <summary>
    /// The chunk ID for the TracksChunk.
    /// </summary>
    public static ReadOnlySpan<byte> ID => "TRKS"u8;

    /// <summary>
    /// Gets the tracks contained in this chunk.
    /// </summary>
    public TrackV1[] Tracks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TracksV1Chunk"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the TracksChunk from.</param>
    /// <param name="size">The size of the TracksChunk in bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    public TracksV1Chunk(Stream stream, int size)
    {
        ArgumentNullException.ThrowIfNull(stream);

        int count = size / TrackV1.Size;
        var tracks = new TrackV1[count];
        for (int i = 0; i < count; i++)
        {
            tracks[i] = new TrackV1(stream);
        }

        Tracks = tracks;
    }
}
