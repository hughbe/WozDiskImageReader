using System.IO.Hashing;
using BenchmarkDotNet.Attributes;
using WozDiskImageReader;
using WozDiskImageReader.Chunks;


namespace WozDiskImageReader.Benchmarks;

/// <summary>
/// Benchmarks for reading and enumerating WOZ disk images.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class WozDiskImageBenchmarks
{
    private byte[] _diskData = null!;
    private const string SampleDiskPath = "Samples/ftp.apple.asimov.net/images/apple3/misc/Catalyst - Installation.woz";

    [GlobalSetup]
    public void Setup()
    {
        // Load the disk image into memory once to avoid I/O overhead in benchmarks
        _diskData = File.ReadAllBytes(SampleDiskPath);
    }

    [Benchmark(Description = "Create WozDiskImage from stream")]
    public WozDiskImage CreateDisk()
    {
        using var stream = new MemoryStream(_diskData);
        return new WozDiskImage(stream);
    }

    [Benchmark(Description = "Enumerate and parse all chunks")]
    public int EnumerateAndParseChunks()
    {
        using var stream = new MemoryStream(_diskData);
        var image = new WozDiskImage(stream);
        int count = 0;

        foreach (var chunk in image.EnumerateChunks())
        {
            var chunkId = chunk.ID.ToString();
            var data = image.GetChunkData(chunk);
            using var chunkStream = new MemoryStream(data);

            if (chunkId == "INFO")
                _ = new InfoChunk(chunkStream);
            else if (chunkId == "TMAP")
                _ = new TrackMapChunk(chunkStream);
            else if (chunkId == "TRKS")
                _ = new TracksV2Chunk(chunkStream, (int)chunk.Size);
            else if (chunkId == "META")
                _ = new MetadataChunk(chunkStream, (int)chunk.Size);
            else if (chunkId == "FLUX")
                _ = new FluxChunk(chunkStream);
            else if (chunkId == "WRIT")
                _ = new WritesChunk(chunkStream, (int)chunk.Size);

            count++;
        }

        return count;
    }

    [Benchmark(Description = "CRC32 of full file data")]
    public uint Crc32FullFile()
    {
        return Crc32.HashToUInt32(_diskData.AsSpan(WozDiskImageHeader.Size));
    }
}
