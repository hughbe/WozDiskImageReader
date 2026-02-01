using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WozDiskImageReader;

/// <summary>
/// An inline array of 4 bytes.
/// </summary>
[InlineArray(Size)]
public struct WozDiskImageChunkID
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 4;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="WozDiskImageChunkID"/> struct.
    /// </summary>
    public WozDiskImageChunkID(ReadOnlySpan<byte> data)
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
    public override string ToString()
    {
        Span<byte> span = AsSpan();
        return Encoding.ASCII.GetString(span);
    }
}
