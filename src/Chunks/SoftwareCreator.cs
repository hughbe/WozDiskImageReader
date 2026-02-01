using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the software creator field in the INFO chunk.
/// </summary>
[InlineArray(Size)]
public struct SoftwareCreator
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 32;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftwareCreator"/> struct.
    /// </summary>
    public SoftwareCreator(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);

    /// <inheritdoc/>
    public override string ToString() => Encoding.UTF8.GetString(AsSpan().TrimEnd((byte)' '));
}