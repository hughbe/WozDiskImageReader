# WOZ Disk Image Dumper

A command-line tool for inspecting WOZ disk image files (version 1 and 2).

## Overview

This tool reads and displays detailed information about WOZ disk image files, which are used to represent Apple II floppy disks as accurate bitstreams. The format is documented at [https://applesaucefdc.com/woz/reference1/](https://applesaucefdc.com/woz/reference1/).

## Usage

```bash
dotnet run --project dumper/WozDiskImageDumper.csproj -- <input-file>
```

Or after building:

```bash
./dumper/bin/Debug/net9.0/WozDiskImageDumper <input-file>
```

## Features

The dumper displays:

- **Header Information**
  - File name and size
  - WOZ signature and version (WOZ1 or WOZ2)
  - CRC32 checksum

- **Chunk Summary**
  - Lists all chunks in the file with their IDs, offsets, and sizes

- **INFO Chunk Details**
  - Version number
  - Disk type (5.25" or 3.5")
  - Write protection status
  - Synchronization information
  - MC3470 fake bit cleaning status
  - Creator software
  - **WOZ2 only fields:**
    - Disk sides (1 or 2)
    - Boot sector format (13-sector, 16-sector, or both)
    - Optimal bit timing (in microseconds)
    - Compatible hardware flags (Apple II, Apple II Plus, Apple IIe, Apple IIc, Apple IIe Enhanced, Apple IIgs, Apple IIc Plus, Apple III, Apple III Plus)
    - Required RAM (in KB)
    - Largest track size (in blocks)

- **TMAP Chunk Details** (Track Map)
  - Quarter-track positions and their mappings to physical tracks
  - Shows which tracks are present vs. empty

- **TRKS Chunk Details** (Track Data)
  - Track-by-track information including:
    - Bytes used
    - Bit count
    - Splice point information (for write operations)
    - Splice nibble and bit count

- **META Chunk Details** (Metadata)
  - Key-value metadata pairs such as:
    - Title
    - Publisher/Developer
    - Copyright information
    - Language
    - RAM requirements
    - Machine compatibility
    - And more

## Example Output

The tool uses Spectre.Console to provide formatted, colorful output with tables displaying all the information in a readable format.

## References

- [WOZ Disk Image Format Specification (Version 1)](https://applesaucefdc.com/woz/reference1/)
- [WOZ Disk Image Format Specification (Version 2)](https://applesaucefdc.com/woz/reference2/)
