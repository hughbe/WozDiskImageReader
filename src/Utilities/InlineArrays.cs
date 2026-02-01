using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WozDiskImageReader.Utilities;

/// <summary>
/// An inline array of 3 bytes.
/// </summary>
[InlineArray(Size)]
public struct ByteArray3
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 3;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray3"/> struct.
    /// </summary>
    public ByteArray3(ReadOnlySpan<byte> data)
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
}

/// <summary>
/// An inline array of 4 bytes.
/// </summary>
[InlineArray(Size)]
public struct ByteArray4
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 4;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray4"/> struct.
    /// </summary>
    public ByteArray4(ReadOnlySpan<byte> data)
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
}

/// <summary>
/// An inline array of 160 bytes.
/// </summary>
[InlineArray(Size)]
public struct ByteArray160
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 160;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray160"/> struct.
    /// </summary>
    public ByteArray160(ReadOnlySpan<byte> data)
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
}
