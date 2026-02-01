using System.Diagnostics;
using WozDiskImageReader.Chunks;

namespace WozDiskImageReader.Tests;

public class WozDiskImageTests
{
    public static TheoryData<string> DiskImages =>
    [
        "ftp.apple.asimov.net/images/apple3/misc/Catalyst - Data.woz",
        "ftp.apple.asimov.net/images/apple3/misc/Catalyst - Installation.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/spreadsheet/Apple III Visicalc III Advanced - Arbeitsdisk.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/spreadsheet/Apple III Visicalc III Advanced - Start disk.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/word_processing/Apple Writer III - Master.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/word_processing/Apple Writer III - Sample Data Files.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/word_processing/Apple Writer III - Utilities.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/word_processing/Apple Writer III (Swedish).woz",
        "ftp.apple.asimov.net/images/apple3/productivity/word_processing/Text Verarbeitungs Program Apple Writer III - Arbeitsdiskette.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/word_processing/Text Verarbeitungs Program Apple Writer III - Dienstprogramme.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/word_processing/Text Verarbeitungs Program Apple Writer III - Sicherung.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/Apple III - PFS Report.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/Apple III - PFS Softwork.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/Senior Analyst III - Boot.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/Senior Analyst III - Data.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/Senior Analyst III - Master backup.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/Senior Analyst III - Master.woz",
        "ftp.apple.asimov.net/images/apple3/productivity/Senior Analyst III - Samples.woz",
    ];

    [Theory]
    [MemberData(nameof(DiskImages))]
    public void Ctor_Stream(string archiveName)
    {
        var filePath = Path.Combine("Samples", archiveName);
        using var stream = File.OpenRead(filePath);
        var image = new WozDiskImage(stream);

        // Create output directory based on archive name (without extension)
        string archiveNameWithoutExtension = Path.GetFileNameWithoutExtension(archiveName);
        string outputDir = Path.Combine("Output", archiveNameWithoutExtension);

        // Delete the output directory if it exists
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        Directory.CreateDirectory(outputDir);

        foreach (var chunk in image.EnumerateChunks())
        {
            Debug.WriteLine($"Chunk: {chunk.ID}, Size: {chunk.Size}");

            switch (chunk.ID)
            {
                case var id when id.AsSpan().SequenceEqual(InfoChunk.ID):
                    var infoChunk = new InfoChunk(stream);
                    Debug.WriteLine($"  Version: {infoChunk.VersionNumber}");
                    Debug.WriteLine($"  Disk Type: {infoChunk.DiskType}");
                    Debug.WriteLine($"  Write Protected: {infoChunk.WriteProtected}");
                    Debug.WriteLine($"  Synchronized: {infoChunk.Synchronized}");
                    Debug.WriteLine($"  Cleaned: {infoChunk.Cleaned}");
                    Debug.WriteLine($"  Creator Software: {infoChunk.CreatorSoftware}");
                    if (infoChunk.VersionNumber >= 2)
                    {
                        Debug.WriteLine($"  Disk Sides: {infoChunk.DiskSides}");
                        Debug.WriteLine($"  Boot Sector Format: {infoChunk.BootSectorFormat}");
                        Debug.WriteLine($"  Optimal Bit Timing: {infoChunk.OptimalBitTiming}");
                        Debug.WriteLine($"  Compatible Hardware: {infoChunk.CompatibleHardware}");
                        Debug.WriteLine($"  Required RAM: {infoChunk.RequiredRAM}");
                        Debug.WriteLine($"  Largest Track: {infoChunk.LargestTrack}");
                    }
                    if (infoChunk.VersionNumber >= 3)
                    {
                        Debug.WriteLine($"  FLUXBlock: {infoChunk.FluxBlock}");
                        Debug.WriteLine($"  LargestFluxTrack: {infoChunk.LargestFluxTrack}");
                    }
                    break;

                case var id when id.AsSpan().SequenceEqual(TrackMapChunk.ID):
                    var trackChunk = new TrackMapChunk(stream);
                    Debug.WriteLine($"  Track Offsets: {string.Join(", ", trackChunk.Entries)}");
                    break;

                case var id when id.AsSpan().SequenceEqual(TracksV1Chunk.ID):
                    if (image.Version == 1)
                    {
                        var trackDataChunk = new TracksV1Chunk(stream, (int)chunk.Size);
                        Debug.WriteLine($"  Number of Tracks: {trackDataChunk.Tracks.Count}");
                        foreach (var track in trackDataChunk.Tracks)
                        {
                            Debug.WriteLine($"    Track - Bytes Used: {track.BytesUsed}, Bit Count: {track.BitCount}, Splice Point: {track.SplicePoint}");
                        }
                    }
                    else
                    {
                        var trackDataChunk = new TracksV2Chunk(stream, (int)chunk.Size);
                        foreach (var track in trackDataChunk.Tracks)
                        {
                            Debug.WriteLine($"    Track - Start Block: {track.StartBlock}, Block Count: {track.BlockCount}, Bit Count: {track.BitCount}");
                        }
                    }

                    break;

                case var id when id.AsSpan().SequenceEqual(MetadataChunk.ID):
                    var metaChunk = new MetadataChunk(stream, (int)chunk.Size);
                    Debug.WriteLine("  Metadata:");
                    foreach (var kvp in metaChunk.Metadata)
                    {
                        Debug.WriteLine($"    {kvp.Key}: {string.Join(", ", kvp.Value ?? [])}");
                    }

                    break;

                case var id when id.AsSpan().SequenceEqual(FluxChunk.ID):
                    var fluxChunk = new FluxChunk(stream);
                    Debug.WriteLine($"  Flux Entries: {string.Join(", ", fluxChunk.Entries)}");
                    break;

                case var id when id.AsSpan().SequenceEqual(WritesChunk.ID):
                    var writesChunk = new WritesChunk(stream, (int)chunk.Size);
                    foreach (var write in writesChunk.WriteTracks)
                    {
                        Debug.WriteLine($"    Write - Track Number: {write.TrackNumber}, Command Count: {write.CommandCount}, AdditionalWriteFunctions: {write.AdditionalWriteFunctions}, Reserved: {write.Reserved}, Checksum: {write.Checksum}");
                        foreach (var command in write.Commands)
                        {
                            Debug.WriteLine($"      Command - StartingBitIndex: {command.StartingBitIndex}, BitCount: {command.BitCount}, LeaderNibble: {command.LeaderNibble}, LeaderBitCount: {command.LeaderBitCount}, LeaderCount: {command.LeaderCount}, Reserved: {command.Reserved}");
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException($"Chunk ID {chunk.ID} not implemented in test.");
            }
        }
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new WozDiskImage(null!));
    }
}
