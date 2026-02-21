using System.Text;

namespace WozDiskImageReader;

/// <summary>
/// Represents a WOZ disk image.
/// </summary>
public class WozDiskImage
{
    private readonly Stream _stream;

    private readonly long _streamStartOffset;

    /// <summary>
    /// Gets the WOZ disk image header.
    /// </summary>
    public WozDiskImageHeader Header { get; }

    /// <summary>
    /// Gets the version of the disk image.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WozDiskImage"/> class.
    /// </summary>
    /// <param name="stream">The stream representing the WOZ disk image.</param>
    public WozDiskImage(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        _stream = stream;
        _streamStartOffset = stream.Position;

        // WOZ files begin with the following 12-byte header in order to identify
        // the file type as well as detect any corruption that may have occurred.
        // The easiest way to detect that a file is indeed a WOZ file is to check
        // the first 8 bytes of the file for the signature. The remaining 4 bytes
        // are a CRC of all remaining data in the file. This is only provided to
        // allow you to ensure file integrity and is not necessary to process the
        // file. If the CRC is 0x00000000, then no CRC has been calculated for the
        // file and should be ignored. The exact CRC routine used is shown in
        // Appendix A, and you should be passing in 0x00000000 as the initial crc
        // value.
        Span<byte> headerBuffer = stackalloc byte[WozDiskImageHeader.Size];
        stream.ReadExactly(headerBuffer);

        Header = new WozDiskImageHeader(headerBuffer);
        Version = Header.Signature switch
        {
            var _ when Header.Signature.AsSpan().SequenceEqual(WozDiskImageHeader.SignatureV1) => 1,
            var _ when Header.Signature.AsSpan().SequenceEqual(WozDiskImageHeader.SignatureV2) => 2,
            _ => throw new NotImplementedException("Unknown Signature.")
        };
    }

    /// <summary>
    /// Enumerates the chunks in the WOZ disk image.
    /// </summary>
    /// <returns>The chunks in the WOZ disk image.</returns>
    public IEnumerable<WozDiskImageChunk> EnumerateChunks()
    {
        // After the header comes a sequence of chunks which each contain information
        // about the disk image. Using chunks allows for the WOZ disk format to
        // provide forward compatibility as chunks can be added to the specification
        // and will just be safely ignored by applications that do not care (or know)
        // about the information. For lower-performance emulation platforms, the
        // primary data chunks are all located in fixed positions so that direct
        // access to data is possible using just offsets from the start of the file.
        _stream.Seek(_streamStartOffset + WozDiskImageHeader.Size, SeekOrigin.Begin);

        while (_stream.Position - _streamStartOffset < _stream.Length)
        {
            var chunk = new WozDiskImageChunk(_stream);

            var position = _stream.Position;
            yield return chunk;
            
            // Skip the chunk data for now; we'll read it in when requested.
            _stream.Seek(position + chunk.Size, SeekOrigin.Begin);
        }
    }

    /// <summary>
    /// Gets the data for the specified chunk.
    /// </summary>
    /// <param name="chunk">The chunk to get the data for.</param>
    /// <returns>The chunk data.</returns>
    /// <exception cref="ArgumentException">Thrown if the chunk data could not be read from the stream.</exception>
    public byte[] GetChunkData(WozDiskImageChunk chunk)
    {
        _stream.Seek(chunk.Offset + WozDiskImageChunk.MinSize, SeekOrigin.Begin);

        var data = new byte[chunk.Size];
        _stream.ReadExactly(data);
        return data;
    }

    /// <summary>
    /// Gets the data for the specified chunk into the provided buffer.
    /// </summary>
    /// <param name="chunk">The chunk to get the data for.</param>
    /// <param name="buffer">The buffer to read the chunk data into.</param>
    /// <returns>The number of bytes read into the buffer.</returns>
    /// <exception cref="ArgumentException">Thrown if the buffer is too small to hold the chunk data.</exception>
    public int GetChunkData(WozDiskImageChunk chunk, Span<byte> buffer)
    {
        if (chunk.Size > int.MaxValue)
        {
            throw new ArgumentException("Chunk size exceeds maximum supported size.", nameof(chunk));
        }
        if (buffer.Length < chunk.Size)
        {
            throw new ArgumentException("Buffer is too small to hold chunk data.", nameof(buffer));
        }

        _stream.Seek(chunk.Offset + WozDiskImageChunk.MinSize, SeekOrigin.Begin);
        _stream.ReadExactly(buffer[..(int)chunk.Size]);
        return (int)chunk.Size;
    }
}
