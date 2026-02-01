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

    /// <summary>
    /// Gets the list of tracks contained in this chunk.
    /// </summary>
    public List<TrackV2> Tracks { get; }

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
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference2/
        var offset = stream.Position;

        // First track in track array. TMAP value of 0.
        // Second track in track array. TMAP value of 1.
        // Third track in track array. TMAP value of 2.
        // Last track in track array. TMAP value of 159.
        var tracks = new List<TrackV2>(160);
        for (int i = 0; i < 160; i++)
        {
            tracks.Add(new TrackV2(stream));
        }

        Tracks = tracks;

        // Start of the actual track bits.
        var remainingLength = size - (stream.Position - offset);
        TrackDataBlocks = new byte[remainingLength];
        if (stream.Read(TrackDataBlocks) != remainingLength)
        {
            throw new ArgumentException("Could not read entire TRACKS chunk data from stream.", nameof(stream));
        }
    }
}