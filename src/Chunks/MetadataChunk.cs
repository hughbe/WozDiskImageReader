using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the Metadata chunk in a WOZ disk image.
/// </summary>
public readonly struct MetadataChunk
{
    /// <summary>
    /// The chunk ID for the MetadataChunk.
    /// </summary>
    public static ReadOnlySpan<byte> ID => "META"u8;

    /// <summary>
    /// Gets the metadata key and value pairs contained in this chunk.
    /// </summary>
    public Dictionary<string, List<string>?> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataChunk"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the MetadataChunk from.</param>
    /// <param name="size">The size of the MetadataChunk in bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    public MetadataChunk(Stream stream, int size)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference1/
        var metadataBuffer = size <= 1024 ? stackalloc byte[size] : new byte[size];
        if (stream.Read(metadataBuffer) != metadataBuffer.Length)
        {
            throw new ArgumentException("Could not read Metadata chunk from stream.", nameof(stream));
        }

        string metadataString = Encoding.UTF8.GetString(metadataBuffer);
        var rows = metadataString.AsSpan().Split('\n');

        var metadata = new Dictionary<string, List<string>?>();
        foreach (var row in rows)
        {
            var rowSpan = metadataString.AsSpan(row);
            var values = rowSpan.Split('\t');
            string? key = null;
            List<string>? keyValues = null;
            foreach (var value in values)
            {
                var (valueOffset, valueLength) = value.GetOffsetAndLength(rowSpan.Length);
                var valueSpan = rowSpan.Slice(valueOffset, valueLength);
                if (key == null)
                {
                    key = valueSpan.ToString();
                }
                else
                {
                    keyValues ??= [];
                    keyValues.Add(valueSpan.ToString());
                }
            }

            if (string.IsNullOrEmpty(key))
            {
                // Ignore blank strings.
                if (keyValues is null)
                {
                    continue;
                }

                throw new ArgumentException("Found empty key in metadata", nameof(stream));
            }
            
            if (!metadata.TryAdd(key, keyValues))
            {
                throw new ArgumentException("Found duplicate key in metadata", nameof(stream));
            }
        }

        Metadata = metadata;
    }
}
