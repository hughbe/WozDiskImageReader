using BenchmarkDotNet.Attributes;
using WozDiskImageReader;

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
}
