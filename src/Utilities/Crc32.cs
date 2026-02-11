using System.IO.Hashing;

namespace WozDiskImageReader.Utilities;

/// <summary>
/// CRC-32 hash algorithm implementation.
/// </summary>
public static class Crc32
{
    /// <summary>
    /// Calculates the CRC-32 checksum for the given data using the standard
    /// CRC-32 (IEEE 802.3) algorithm.
    /// </summary>
    /// <param name="data">The data to calculate the checksum for.</param>
    /// <param name="initialState">The initial CRC state (default is 0).</param>
    /// <returns>The calculated CRC-32 checksum.</returns>
    public static uint Calculate(ReadOnlySpan<byte> data, uint initialState = 0u)
    {
        return System.IO.Hashing.Crc32.HashToUInt32(data);
    }
}
