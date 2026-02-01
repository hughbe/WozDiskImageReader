using System.Buffers.Binary;
using System.Diagnostics;
using WozDiskImageReader.Utilities;

namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the INFO chunk in a WOZ disk image.
/// </summary>
public readonly struct InfoChunk
{
    /// <summary>
    /// Gets the chunk ID for the INFO chunk.
    /// </summary>
    public static ReadOnlySpan<byte> ID => "INFO"u8;

    /// <summary>
    /// The size of the INFO chunk in bytes.
    /// </summary>
    public const int Size = 60;

    /// <summary>
    /// Gets the version number of the INFO chunk.
    /// </summary>
    public byte VersionNumber { get; }

    /// <summary>
    /// Gets the disk type specified in the INFO chunk.
    /// </summary>
    public DiskType DiskType { get; }

    /// <summary>
    /// Gets a value indicating whether the disk is write-protected.
    /// </summary>
    public byte WriteProtected { get; }

    /// <summary>
    /// Gets a value indicating whether cross track sync was used during imaging.
    /// </summary>
    public byte Synchronized { get; }

    /// <summary>
    /// Gets a value indicating whether MC3470 fake bits have been removed.
    /// </summary>
    public byte Cleaned { get; }

    /// <summary>
    /// Gets the name of the software that created the WOZ file.
    /// </summary>
    public SoftwareCreator CreatorSoftware { get; }

    /// <summary>
    /// Gets the number of disk sides contained within this image.
    /// A 5.25 disk will always be 1. A 3.5 disk can be 1 or 2.
    /// </summary>
    public byte? DiskSides { get; }

    /// <summary>
    /// Gets the type of boot sector found on this disk.
    /// This is only for 5.25 disks.
    /// 3.5 disks should just set this to 0.
    /// </summary>
    public BootSectorFormat? BootSectorFormat { get; }

    /// <summary>
    /// Gets the ideal rate that bits should be delivered to the disk controller
    /// card. This value is in 125 nanosecond increments, so 8 is equal to 1
    /// microsecond. And a standard bit rate for a 5.25 disk would be 32 (4µs).
    /// </summary>
    public byte? OptimalBitTiming { get; }

    /// <summary>
    /// Gets the bit field with a 1 indicating known compatibility.
    /// Multiple compatibility flags are possible.
    /// A 0 value represents that the compatible hardware list is unknown.
    /// </summary>
    public CompatibleHardware? CompatibleHardware { get; }

    /// <summary>
    /// Gets the minimum RAM size needed for this software. This value is in K
    /// (1024 bytes). If the minimum size is unknown, this value should be set
    /// to 0. So, a requirement of 64K would be indicated by the value 64 here.
    /// </summary>
    public ushort? RequiredRAM { get; }

    /// <summary>
    /// Gets the number of blocks (512 bytes) used by the largest track.
    /// Can be used to allocate a buffer with a size safe for all tracks.
    /// </summary>
    public ushort? LargestTrack { get; }

    /// <summary>
    /// Gets the block number where the FLUX chunk resides relative to the
    /// start of the file.
    /// </summary>
    public ushort? FluxBlock { get; }

    /// <summary>
    /// Gets the number of blocks (512 bytes) used by the largest flux track.
    /// Can be used to allocate a buffer with a size safe for all tracks.
    /// </summary>
    public ushort? LargestFluxTrack { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InfoChunk"/> struct by reading from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the INFO chunk from.</param>
    /// <exception cref="ArgumentException">Thrown when the chunk cannot be read or contains invalid data.</exception>
    /// <exception cref="NotSupportedException">Thrown when the INFO chunk version is not supported.</exception>
    public InfoChunk(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        Span<byte> buffer = stackalloc byte[Size];
        if (stream.Read(buffer) != Size)
        {
            throw new ArgumentException("Could not read entire INFO chunk from stream.", nameof(stream));
        }

        // Structure documented in https://applesaucefdc.com/woz/reference1/
        // and https://applesaucefdc.com/woz/reference2/
        int offset = 0;

        // Version number of the INFO chunk. Current version is 1.
        VersionNumber = buffer[offset];
        offset += 1;

        if (VersionNumber < 1 || VersionNumber > 3)
        {
            throw new NotSupportedException($"Unsupported INFO chunk version number: {VersionNumber}");
        }

        // 1 = 5.25, 2 = 3.5
        DiskType = (DiskType)buffer[offset];
        offset += 1;

        if (!Enum.IsDefined(DiskType))
        {
            throw new ArgumentException($"Invalid disk type value in INFO chunk: {(byte)DiskType}", nameof(stream));
        }

        // 1 = Floppy is write protected
        WriteProtected = buffer[offset];
        offset += 1;

        // 1 = Cross track sync was used during imaging
        Synchronized = buffer[offset];
        offset += 1;

        // 1 = MC3470 fake bits have been removed
        Cleaned = buffer[offset];
        offset += 1;

        // Name of software that created the WOZ file. String in UTF-8. No BOM.
        // Padded to 32 bytes using space character (0x20).
        // ex: “Applesauce v1.0                 ”
        CreatorSoftware = new SoftwareCreator(buffer.Slice(offset, SoftwareCreator.Size));
        offset += SoftwareCreator.Size;

        Debug.Assert(offset == 37, "Offset should be 37 after reading version 1 fields");

        if (VersionNumber >= 2)
        {
            // The number of disk sides contained within this image. A 5.25 disk
            // will always be 1. A 3.5 disk can be 1 or 2.
            DiskSides = buffer[offset];
            offset += 1;

            if (DiskSides < 1 || DiskSides > (DiskType == DiskType.ThreeAndHalfInch ? 2 : 1))
            {
                throw new ArgumentException($"Invalid disk sides value in INFO chunk: {DiskSides}", nameof(stream));
            }

            // The type of boot sector found on this disk. This is only for 5.25
            // disks. 3.5 disks should just set this to 0.
            // 0 = Unknown
            // 1 = Contains boot sector for 16-sector
            // 2 = Contains boot sector for 13-sector
            // 3 = Contains boot sectors for both
            BootSectorFormat = (BootSectorFormat)buffer[offset];
            offset += 1;

            // The ideal rate that bits should be delivered to the disk controller
            // card. This value is in 125 nanosecond increments, so 8 is equal to
            // 1 microsecond. And a standard bit rate for a 5.25 disk would be 32
            // (4µs).
            OptimalBitTiming = buffer[offset];
            offset += 1;

            // Bit field with a 1 indicating known compatibility. Multiple
            // compatibility flags are possible. A 0 value represents that the
            // compatible hardware list is unknown.
            // 0x0001 = Apple II
            // 0x0002 = Apple II Plus
            // 0x0004 = Apple IIe (unenhanced)
            // 0x0008 = Apple IIc
            // 0x0010 = Apple IIe Enhanced
            // 0x0020 = Apple IIgs
            // 0x0040 = Apple IIc Plus
            // 0x0080 = Apple III
            // 0x0100 = Apple III Plus
            CompatibleHardware = (CompatibleHardware)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
            offset += 2;

            // Minimum RAM size needed for this software. This value is in K (1024
            // bytes). If the minimum size is unknown, this value should be set to
            // 0. So, a requirement of 64K would be indicated by the value 64 here.
            RequiredRAM = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
            offset += 2;

            // The number of blocks (512 bytes) used by the largest track. Can be
            // used to allocate a buffer with a size safe for all tracks.
            LargestTrack = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));
            offset += 2;
        }
        else
        {
            DiskSides = null;
            BootSectorFormat = null;
            OptimalBitTiming = null;
            CompatibleHardware = null;
            RequiredRAM = null;
            LargestTrack = null;
            offset += 9; // Skip unused bytes.
        }

        Debug.Assert(offset == 46, "Offset should be 46 after reading version 2 fields.");

        if (VersionNumber >= 3)
        {
            // Block number where the FLUX chuck resides relative to the start
            // of the file. A FLUX chunk always occupies its own block. If this
            // WOZ does not utilize a FLUX chunk, then this value will be 0.
            // When checking for the existence of a FLUX chunk, make sure that
            // BOTH this value and the next one (Largest Flux Track) are non-zero.
            FluxBlock = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
            offset += 2;

            // The number of blocks (512 bytes) used by the largest flux track.
            // Can be used to allocate a buffer with a size safe for all tracks.
            LargestFluxTrack = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
            offset += 2;
        }
        else
        {
            FluxBlock = null;
            LargestFluxTrack = null;
            offset += 4; // Skip unused bytes.
        }

        offset += 10; // Skip reserved bytes.

        Debug.Assert(offset == buffer.Length, "Did not read entire INFO chunk.");
    }
}
