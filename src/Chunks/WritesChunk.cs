namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the WRITES chunk in a WOZ disk image.
/// </summary>
public readonly struct WritesChunk
{
    /// <summary>
    /// The chunk ID for the WritesChunk.
    /// </summary>
    public static ReadOnlySpan<byte> ID => "WRIT"u8;

    /// <summary>
    /// Gets the list of write tracks contained in this chunk.
    /// </summary>
    public List<WriteTrack> WriteTracks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WritesChunk"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the WritesChunk from.</param>
    /// <param name="size">The size of the WritesChunk in bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    public WritesChunk(Stream stream, int size)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Structure documented in https://applesaucefdc.com/woz/reference2/
        var offset = stream.Position;

        var writeTracks = new List<WriteTrack>();
        while (stream.Position - offset < size)
        {
            writeTracks.Add(new WriteTrack(stream));
        }

        WriteTracks = writeTracks;
    }
}
