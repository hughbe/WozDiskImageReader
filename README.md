# WozDiskImageReader

A lightweight .NET library for reading WOZ disk image files (.woz). WOZ is a modern disk image format designed to preserve Apple II floppy disks as accurate bitstreams, including flux-level data, enabling high-fidelity emulation and archival.

## Features

- Read WOZ1 and WOZ2 disk images (.woz files)
- Parse all standard chunk types:
  - INFO - Disk metadata (type, write protection, creator software, compatible hardware)
  - TMAP - Quarter-track to physical track mapping
  - TRKS - Track bitstream data (both V1 and V2 formats)
  - META - Key-value metadata (title, publisher, copyright, etc.)
  - FLUX - Flux transition timing data
  - WRIT - Write operation data
- Support for 5.25" and 3.5" disk types
- CRC-32 integrity verification
- Hardware compatibility detection (Apple II, II Plus, IIe, IIc, IIgs, III, and more)
- Zero external dependencies (core library uses only System.IO.Hashing)
- Built for .NET 9.0

## Installation

Add the project reference to your .NET application:

```sh
dotnet add reference path/to/WozDiskImageReader.csproj
```

Or, if published on NuGet:

```sh
dotnet add package WozDiskImageReader
```

## Usage

### Opening a WOZ Disk Image

```csharp
using WozDiskImageReader;

// Open a WOZ disk image file
using var stream = File.OpenRead("disk.woz");

// Parse the disk image
var image = new WozDiskImage(stream);

// Get header information
Console.WriteLine($"Version: WOZ{image.Version}");
Console.WriteLine($"CRC32: 0x{image.Header.Crc:X8}");
```

### Enumerating Chunks

```csharp
// List all chunks in the disk image
foreach (var chunk in image.EnumerateChunks())
{
    Console.WriteLine($"Chunk: {chunk.ID}");
    Console.WriteLine($"  Offset: 0x{chunk.Offset:X}");
    Console.WriteLine($"  Size: {chunk.Size} bytes");
}
```

### Reading the INFO Chunk

```csharp
using WozDiskImageReader.Chunks;

foreach (var chunk in image.EnumerateChunks())
{
    if (chunk.ID.ToString() == "INFO")
    {
        var data = image.GetChunkData(chunk);
        var info = new InfoChunk(new MemoryStream(data));

        Console.WriteLine($"Disk Type: {info.DiskType}");
        Console.WriteLine($"Write Protected: {info.WriteProtected == 1}");
        Console.WriteLine($"Creator: {info.CreatorSoftware}");

        if (info.CompatibleHardware.HasValue)
        {
            Console.WriteLine($"Compatible Hardware: {info.CompatibleHardware.Value}");
        }
    }
}
```

### Reading Track Data (WOZ1)

```csharp
foreach (var chunk in image.EnumerateChunks())
{
    if (chunk.ID.ToString() == "TRKS")
    {
        var data = image.GetChunkData(chunk);
        var trks = new TracksV1Chunk(new MemoryStream(data), (int)chunk.Size);

        for (int i = 0; i < trks.Tracks.Count; i++)
        {
            var track = trks.Tracks[i];
            if (track.BytesUsed > 0)
            {
                Console.WriteLine($"Track {i}: {track.BitCount} bits, {track.BytesUsed} bytes");
            }
        }
    }
}
```

### Reading Metadata

```csharp
foreach (var chunk in image.EnumerateChunks())
{
    if (chunk.ID.ToString() == "META")
    {
        var data = image.GetChunkData(chunk);
        var meta = new MetadataChunk(new MemoryStream(data), (int)chunk.Size);

        foreach (var kvp in meta.Metadata)
        {
            Console.WriteLine($"{kvp.Key}: {string.Join(", ", kvp.Value ?? [])}");
        }
    }
}
```

## API Overview

### WozDiskImage

The main class for reading WOZ disk images.

- `WozDiskImage(Stream stream)` - Opens a disk image from a seekable, readable stream
- `Header` - Gets the 12-byte file header
- `Version` - Gets the WOZ version (1 or 2)
- `EnumerateChunks()` - Lazily enumerates chunks in the file
- `GetChunkData(WozDiskImageChunk)` - Reads chunk data as a byte array
- `GetChunkData(WozDiskImageChunk, Span<byte>)` - Reads chunk data into a buffer

### WozDiskImageHeader

The 12-byte file header:

- `Signature` - 4-byte signature ("WOZ1" or "WOZ2")
- `HighBit` - High bit validation byte (0xFF)
- `FileTranslator` - 3-byte file translation detection sequence
- `Crc` - CRC-32 of all remaining data (0 if not calculated)

### WozDiskImageChunk

Represents a chunk within the file:

- `Offset` - Byte offset of the chunk in the stream
- `ID` - 4-byte ASCII chunk identifier
- `Size` - Size of the chunk data in bytes

### InfoChunk

Disk metadata (60 bytes):

- `VersionNumber` - INFO chunk version (1, 2, or 3)
- `DiskType` - 5.25" or 3.5" disk
- `WriteProtected` - Whether the disk is write-protected
- `Synchronized` - Whether cross-track sync was used during imaging
- `Cleaned` - Whether MC3470 fake bits have been removed
- `CreatorSoftware` - Name of the software that created the WOZ file
- `DiskSides` - Number of disk sides (WOZ2+)
- `BootSectorFormat` - Boot sector type for 5.25" disks (WOZ2+)
- `OptimalBitTiming` - Ideal bit delivery rate in 125ns increments (WOZ2+)
- `CompatibleHardware` - Compatible Apple hardware flags (WOZ2+)
- `RequiredRAM` - Minimum RAM in KB (WOZ2+)
- `LargestTrack` - Largest track size in 512-byte blocks (WOZ2+)

### Enumerations

- `DiskType` - `FiveAndQuarterInch`, `ThreeAndHalfInch`
- `BootSectorFormat` - `Unknown`, `Sixteen`, `Thirteen`, `Both`
- `CompatibleHardware` (Flags) - `AppleII`, `AppleIIPlus`, `AppleIIeUnenhanced`, `AppleIIc`, `AppleIIeEnhanced`, `AppleIIgs`, `AppleIIcPlus`, `AppleIII`, `AppleIIIPlus`

## Building

Build the project using the .NET SDK:

```sh
dotnet build
```

Run tests:

```sh
dotnet test
```

Run benchmarks:

```sh
dotnet run --project benchmarks/WozDiskImageReader.Benchmarks.csproj -c Release
```

## WozDiskImageDumper CLI

Inspect WOZ disk images using the command-line dumper tool. It displays header information, chunk details, track maps, and metadata in formatted tables.

### Build

```sh
dotnet build dumper/WozDiskImageDumper.csproj -c Release
```

### Usage

```sh
dotnet run --project dumper/WozDiskImageDumper.csproj -- <input>
```

**Arguments:**
- `<input>`: Path to the WOZ disk image file (.woz)

## Requirements

- .NET 9.0 or later

## License

MIT License. See [LICENSE](LICENSE) for details.

Copyright (c) 2026 Hugh Bellamy

## About WOZ Disk Images

The WOZ format was created by John K. Morris for the [Applesauce](https://applesaucefdc.com/) floppy disk controller. It represents Apple II floppy disks as accurate bitstreams rather than decoded sector data, enabling:

- Bit-accurate preservation of original disk content
- Support for copy-protected disks
- Flux-level timing data for maximum fidelity
- Quarter-track resolution for precise head positioning
- Metadata preservation (title, publisher, notes)
- Forward compatibility through a chunk-based structure

**Format Characteristics:**
- 12-byte header with signature and CRC-32 integrity check
- Chunk-based structure (INFO, TMAP, TRKS, META, FLUX, WRIT)
- Support for 5.25" and 3.5" floppy disks
- Track data stored as raw bitstreams
- Hardware compatibility flags for Apple II family

**References:**
- [WOZ Disk Image Reference (v1)](https://applesaucefdc.com/woz/reference1/)
- [WOZ Disk Image Reference (v2)](https://applesaucefdc.com/woz/reference2/)

## Related Projects

- [AppleDiskImageReader](https://github.com/hughbe/AppleDiskImageReader) - Reader for Apple II universal disk (.2mg) images
- [AppleIIDiskReader](https://github.com/hughbe/AppleIIDiskReader) - Reader for Apple II DOS 3.3 disk (.dsk) images
- [ProDosVolumeReader](https://github.com/hughbe/ProDosVolumeReader) - Reader for ProDOS (.po) volumes
- [DiskCopyReader](https://github.com/hughbe/DiskCopyReader) - Reader for Disk Copy 4.2 (.dc42) images
- [MfsReader](https://github.com/hughbe/MfsReader) - Reader for MFS (Macintosh File System) volumes
- [HfsReader](https://github.com/hughbe/HfsReader) - Reader for HFS (Hierarchical File System) volumes
- [ResourceForkReader](https://github.com/hughbe/ResourceForkReader) - Reader for Macintosh resource forks
- [StuffItReader](https://github.com/hughbe/StuffItReader) - Reader for StuffIt (.sit) archives
- [ShrinkItReader](https://github.com/hughbe/ShrinkItReader) - Reader for ShrinkIt (.shk, .sdk) archives
