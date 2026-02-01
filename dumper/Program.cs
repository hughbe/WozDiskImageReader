using Spectre.Console;
using Spectre.Console.Cli;
using WozDiskImageReader;
using WozDiskImageReader.Chunks;
using WozDiskImageReader.Utilities;

public sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<DumpCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("woz-disk-image-dumper");
            config.ValidateExamples();
        });

        return app.Run(args);
    }
}

sealed class DumpSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    public required string Input { get; init; }
}

sealed class DumpCommand : Command<DumpSettings>
{
    public override int Execute(CommandContext context, DumpSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {input.FullName}");
            return -1;
        }

        WozDiskImage image;
        try
        {
            using var stream = input.OpenRead();
            image = new WozDiskImage(stream);

            // Display header information
            DisplayHeader(input, image, stream);

            // Display chunk information
            DisplayChunks(image);
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or NotSupportedException or NotImplementedException)
        {
            AnsiConsole.MarkupLine($"[red]Failed to parse WOZ disk image[/]: {Markup.Escape(ex.Message)}");
            return -1;
        }

        return 0;
    }

    private static void DisplayHeader(FileInfo file, WozDiskImage image, Stream stream)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold yellow]WOZ Disk Image Header[/]");

        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("File", Markup.Escape(file.Name));
        table.AddRow("Size", FormatSize(file.Length));
        table.AddRow("Signature", System.Text.Encoding.ASCII.GetString(image.Header.Signature.AsSpan()));
        table.AddRow("Version", $"WOZ{image.Version}");

        if (image.Header.Crc == 0)
        {
            table.AddRow("CRC32", "[dim]Not calculated[/]");
        }
        else
        {
            stream.Seek(WozDiskImageHeader.Size, SeekOrigin.Begin);
            var remainingData = new byte[stream.Length - WozDiskImageHeader.Size];
            stream.ReadExactly(remainingData);
            var calculatedCrc = Crc32.Calculate(remainingData);

            if (calculatedCrc == image.Header.Crc)
            {
                table.AddRow("CRC32", $"0x{image.Header.Crc:X8} [green](valid)[/]");
            }
            else
            {
                table.AddRow("CRC32", $"0x{image.Header.Crc:X8} [red](invalid, calculated 0x{calculatedCrc:X8})[/]");
            }
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayChunks(WozDiskImage image)
    {
        var chunks = image.EnumerateChunks().ToList();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold yellow]Chunks[/]");

        table.AddColumn("ID");
        table.AddColumn("Offset", c => c.Alignment(Justify.Right));
        table.AddColumn("Size", c => c.Alignment(Justify.Right));

        foreach (var chunk in chunks)
        {
            table.AddRow(
                chunk.ID.ToString(),
                $"0x{chunk.Offset:X}",
                FormatSize(chunk.Size));
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Display detailed information for each chunk type
        foreach (var chunk in chunks)
        {
            var chunkId = chunk.ID.ToString();

            if (chunkId == "INFO")
            {
                DisplayInfoChunk(image, chunk);
            }
            else if (chunkId == "TMAP")
            {
                DisplayTrackMapChunk(image, chunk);
            }
            else if (chunkId == "TRKS")
            {
                DisplayTracksChunk(image, chunk);
            }
            else if (chunkId == "FLUX")
            {
                DisplayFluxChunk(image, chunk);
            }
            else if (chunkId == "WRIT")
            {
                DisplayWritesChunk(image, chunk);
            }
            else if (chunkId == "META")
            {
                DisplayMetadataChunk(image, chunk);
            }
        }
    }

    private static void DisplayInfoChunk(WozDiskImage image, WozDiskImageChunk chunk)
    {
        var data = image.GetChunkData(chunk);
        var info = new InfoChunk(new MemoryStream(data));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]INFO Chunk[/]");

        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Version", info.VersionNumber.ToString());
        table.AddRow("Disk Type", info.DiskType.ToString());
        table.AddRow("Write Protected", info.WriteProtected == 1 ? "[red]Yes[/]" : "[green]No[/]");
        table.AddRow("Synchronized", info.Synchronized == 1 ? "Yes" : "No");
        table.AddRow("Cleaned", info.Cleaned == 1 ? "Yes (MC3470 fake bits removed)" : "No");
        table.AddRow("Creator", info.CreatorSoftware.ToString().Trim());

        // WOZ2 specific fields
        if (info.DiskSides.HasValue)
        {
            table.AddRow("Disk Sides", info.DiskSides.Value.ToString());
        }

        if (info.BootSectorFormat.HasValue)
        {
            table.AddRow("Boot Sector", info.BootSectorFormat.Value.ToString());
        }

        if (info.OptimalBitTiming.HasValue)
        {
            var microseconds = info.OptimalBitTiming.Value * 0.125;
            table.AddRow("Optimal Bit Timing", $"{info.OptimalBitTiming.Value} ({microseconds:0.##}µs)");
        }

        if (info.CompatibleHardware.HasValue)
        {
            var hardware = info.CompatibleHardware.Value;
            if (hardware == WozDiskImageReader.Chunks.CompatibleHardware.Unknown)
            {
                table.AddRow("Compatible Hardware", "[dim]Unknown[/]");
            }
            else
            {
                var compatibleList = GetCompatibleHardwareList(hardware);
                table.AddRow("Compatible Hardware", string.Join(", ", compatibleList));
            }
        }

        if (info.RequiredRAM.HasValue && info.RequiredRAM.Value > 0)
        {
            table.AddRow("Required RAM", $"{info.RequiredRAM.Value}K");
        }

        if (info.LargestTrack.HasValue && info.LargestTrack.Value > 0)
        {
            table.AddRow("Largest Track", $"{info.LargestTrack.Value} blocks ({info.LargestTrack.Value * 512} bytes)");
        }

        // WOZ3 specific fields
        if (info.FluxBlock.HasValue && info.FluxBlock.Value > 0)
        {
            table.AddRow("Flux Block", $"{info.FluxBlock.Value}");
        }

        if (info.LargestFluxTrack.HasValue && info.LargestFluxTrack.Value > 0)
        {
            table.AddRow("Largest Flux Track", $"{info.LargestFluxTrack.Value} blocks ({info.LargestFluxTrack.Value * 512} bytes)");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static List<string> GetCompatibleHardwareList(WozDiskImageReader.Chunks.CompatibleHardware hardware)
    {
        var list = new List<string>();

        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleII))
            list.Add("Apple II");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIIPlus))
            list.Add("Apple II Plus");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIIeUnenhanced))
            list.Add("Apple IIe");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIIc))
            list.Add("Apple IIc");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIIeEnhanced))
            list.Add("Apple IIe Enhanced");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIIgs))
            list.Add("Apple IIgs");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIIcPlus))
            list.Add("Apple IIc Plus");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIII))
            list.Add("Apple III");
        if (hardware.HasFlag(WozDiskImageReader.Chunks.CompatibleHardware.AppleIIIPlus))
            list.Add("Apple III Plus");

        return list;
    }

    private static void DisplayTrackMapChunk(WozDiskImage image, WozDiskImageChunk chunk)
    {
        var data = image.GetChunkData(chunk);
        var tmap = new TrackMapChunk(new MemoryStream(data));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]TMAP Chunk (Track Map)[/]");

        table.AddColumn("Quarter Track", c => c.Alignment(Justify.Right));
        table.AddColumn("Track Index", c => c.Alignment(Justify.Right));

        var entries = tmap.Entries.AsSpan();
        for (int i = 0; i < entries.Length; i++)
        {
            var trackIndex = entries[i];
            if (trackIndex == 0xFF)
            {
                continue;
            }

            var quarterTrack = i / 4.0;
            table.AddRow($"{quarterTrack:0.00}", trackIndex.ToString());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayTracksChunk(WozDiskImage image, WozDiskImageChunk chunk)
    {
        var data = image.GetChunkData(chunk);

        if (image.Version >= 2)
        {
            DisplayTracksV2Chunk(data, (int)chunk.Size);
        }
        else
        {
            DisplayTracksV1Chunk(data, (int)chunk.Size);
        }
    }

    private static void DisplayTracksV1Chunk(byte[] data, int size)
    {
        var trks = new TracksV1Chunk(new MemoryStream(data), size);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold cyan]TRKS Chunk - WOZ1 ({trks.Tracks.Count} tracks)[/]");

        table.AddColumn("Track #", c => c.Alignment(Justify.Right));
        table.AddColumn("Bytes Used", c => c.Alignment(Justify.Right));
        table.AddColumn("Bit Count", c => c.Alignment(Justify.Right));
        table.AddColumn("Splice Point", c => c.Alignment(Justify.Right));
        table.AddColumn("Splice Info");

        for (int i = 0; i < trks.Tracks.Count; i++)
        {
            var track = trks.Tracks[i];

            if (track.BytesUsed == 0)
            {
                // Skip empty tracks
                continue;
            }

            var splicePoint = track.SplicePoint == 0xFFFF ? "[dim]None[/]" : track.SplicePoint.ToString();
            var spliceInfo = track.SplicePoint == 0xFFFF
                ? "[dim]N/A[/]"
                : $"Nibble: 0x{track.SpliceNibble:X2}, Bits: {track.SpliceBitCount}";

            table.AddRow(
                i.ToString(),
                track.BytesUsed.ToString(),
                track.BitCount.ToString(),
                splicePoint,
                spliceInfo);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayTracksV2Chunk(byte[] data, int size)
    {
        var trks = new TracksV2Chunk(new MemoryStream(data), size);

        var nonEmptyTracks = trks.Tracks.Where(t => t.BitCount > 0).Count();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold cyan]TRKS Chunk - WOZ2 ({nonEmptyTracks} non-empty tracks)[/]");

        table.AddColumn("Track #", c => c.Alignment(Justify.Right));
        table.AddColumn("Start Block", c => c.Alignment(Justify.Right));
        table.AddColumn("Block Count", c => c.Alignment(Justify.Right));
        table.AddColumn("Bit Count", c => c.Alignment(Justify.Right));
        table.AddColumn("Data Size");

        for (int i = 0; i < trks.Tracks.Count; i++)
        {
            var track = trks.Tracks[i];

            if (track.BitCount == 0)
            {
                // Skip empty tracks
                continue;
            }

            table.AddRow(
                i.ToString(),
                track.StartBlock.ToString(),
                track.BlockCount.ToString(),
                track.BitCount.ToString(),
                FormatSize(track.BlockCount * 512));
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayFluxChunk(WozDiskImage image, WozDiskImageChunk chunk)
    {
        var data = image.GetChunkData(chunk);
        var flux = new FluxChunk(new MemoryStream(data));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]FLUX Chunk (Flux Map)[/]");

        table.AddColumn("Quarter Track", c => c.Alignment(Justify.Right));
        table.AddColumn("Track Index", c => c.Alignment(Justify.Right));

        var entries = flux.Entries.AsSpan();
        for (int i = 0; i < entries.Length; i++)
        {
            var trackIndex = entries[i];
            if (trackIndex == 0xFF)
            {
                continue;
            }

            var quarterTrack = i / 4.0;
            table.AddRow($"{quarterTrack:0.00}", trackIndex.ToString());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayWritesChunk(WozDiskImage image, WozDiskImageChunk chunk)
    {
        var data = image.GetChunkData(chunk);
        var writ = new WritesChunk(new MemoryStream(data), (int)chunk.Size);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold cyan]WRIT Chunk ({writ.WriteTracks.Count} write tracks)[/]");

        table.AddColumn("Track #", c => c.Alignment(Justify.Right));
        table.AddColumn("Commands", c => c.Alignment(Justify.Right));
        table.AddColumn("Functions");
        table.AddColumn("Checksum");

        foreach (var writeTrack in writ.WriteTracks)
        {
            var functions = writeTrack.AdditionalWriteFunctions == WriteTrackFunctions.None
                ? "[dim]None[/]"
                : writeTrack.AdditionalWriteFunctions.ToString();

            table.AddRow(
                writeTrack.TrackNumber.ToString(),
                writeTrack.CommandCount.ToString(),
                functions,
                $"0x{writeTrack.Checksum:X8}");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Display individual write commands for each track
        foreach (var writeTrack in writ.WriteTracks)
        {
            if (writeTrack.Commands.Count == 0)
            {
                continue;
            }

            var cmdTable = new Table()
                .Border(TableBorder.Rounded)
                .Title($"[bold cyan]Write Commands for Track {writeTrack.TrackNumber}[/]");

            cmdTable.AddColumn("Cmd #", c => c.Alignment(Justify.Right));
            cmdTable.AddColumn("Start Bit", c => c.Alignment(Justify.Right));
            cmdTable.AddColumn("Bit Count", c => c.Alignment(Justify.Right));
            cmdTable.AddColumn("Leader Nibble");
            cmdTable.AddColumn("Leader Bits", c => c.Alignment(Justify.Right));
            cmdTable.AddColumn("Leader Count", c => c.Alignment(Justify.Right));

            for (int i = 0; i < writeTrack.Commands.Count; i++)
            {
                var cmd = writeTrack.Commands[i];
                var leaderNibble = cmd.LeaderNibble == 0x00
                    ? "[dim]None[/]"
                    : $"0x{cmd.LeaderNibble:X2}";

                cmdTable.AddRow(
                    i.ToString(),
                    cmd.StartingBitIndex.ToString(),
                    cmd.BitCount.ToString(),
                    leaderNibble,
                    cmd.LeaderBitCount.ToString(),
                    cmd.LeaderCount.ToString());
            }

            AnsiConsole.Write(cmdTable);
            AnsiConsole.WriteLine();
        }
    }

    private static void DisplayMetadataChunk(WozDiskImage image, WozDiskImageChunk chunk)
    {
        var data = image.GetChunkData(chunk);
        var meta = new MetadataChunk(new MemoryStream(data), (int)chunk.Size);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]META Chunk (Metadata)[/]");

        table.AddColumn("Key");
        table.AddColumn("Values");

        foreach (var kvp in meta.Metadata.OrderBy(x => x.Key))
        {
            var values = kvp.Value == null || kvp.Value.Count == 0
                ? "[dim]N/A[/]"
                : Markup.Escape(string.Join(", ", kvp.Value));

            table.AddRow(Markup.Escape(kvp.Key), values);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

}
