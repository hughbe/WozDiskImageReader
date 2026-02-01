using System.Buffers.Binary;
using System.Data.Common;
using System.Diagnostics;
using WozDiskImageReader.Utilities;

namespace WozDiskImageReader;

/// <summary>
/// Represents the header of a WOZ disk image.
/// </summary>
public readonly struct WozDiskImageHeader
{
    /// <summary>
    /// Size of the WOZ disk image header in bytes.
    /// </summary>
    public const int Size = 12;

    /// <summary>
    /// The signature for version 1 ("WOZ1").
    /// </summary>
    public static ReadOnlySpan<byte> SignatureV1 => [0x57, 0x4F, 0x5A, 0x31];

    /// <summary>
    /// The signature for version 2 ("WOZ2").
    /// </summary>
    public static ReadOnlySpan<byte> SignatureV2 => [0x57, 0x4F, 0x5A, 0x32];

    /// <summary>
    /// Gets the signature.
    /// </summary>
    public ByteArray4 Signature { get; }

    /// <summary>
    /// Gets the high bit.
    /// </summary>
    public byte HighBit { get; }

    /// <summary>
    /// Gets the file translator.
    /// </summary>
    public ByteArray3 FileTranslator { get; }

    /// <summary>
    /// Gets the CRC32 of all remaining data in the file.
    /// </summary>
    public uint Crc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WozDiskImageHeader"/> struct.
    /// </summary>
    /// <param name="data">The header data.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid.</exception>
    public WozDiskImageHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference1/
        // and https://applesaucefdc.com/woz/reference2
        int offset = 0;

        // The ASCII string ‘WOZ1’. 0x315A4F57
        // The ASCII string ‘WOZ2’. 0x325A4F57
        Signature = new ByteArray4(data.Slice(offset, ByteArray4.Size));
        offset += ByteArray4.Size;

        if (!Signature.AsSpan().SequenceEqual(SignatureV1) &&
            !Signature.AsSpan().SequenceEqual(SignatureV2))
        {
            throw new ArgumentException("Invalid WOZ disk image signature.", nameof(data));
        }

        // Make sure that high bits are valid (no 7-bit data transmission)
        HighBit = data[offset];
        offset += 1;

        if (HighBit != 0xFF)
        {
            throw new ArgumentException("Invalid WOZ disk image high bit value.", nameof(data));
        }

        // LF CR LF – File translators will often try to convert these.
        FileTranslator = new ByteArray3(data.Slice(offset, ByteArray3.Size));
        offset += ByteArray3.Size;

        ReadOnlySpan<byte> expectedFileTranslator = [0x0A, 0x0D, 0x0A];
        if (!FileTranslator.AsSpan().SequenceEqual(expectedFileTranslator))
        {
            throw new ArgumentException("Invalid WOZ disk image verification bytes.", nameof(data));
        }
        
        // CRC32 of all remaining data in the file. The method used to generate the CRC is described in Appendix A.
        Crc = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        Debug.Assert(offset == data.Length, "Did not consume all data for WOZ disk image header.");
    }
}
