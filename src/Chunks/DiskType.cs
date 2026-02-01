namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents the disk type in a WOZ disk image.
/// </summary>
public enum DiskType : byte
{
    /// <summary>
    /// 5.25-inch disk type.
    /// </summary>
    FiveAndQuarterInch = 1,

    /// <summary>
    /// 3.5-inch disk type.
    /// </summary>
    ThreeAndHalfInch = 2
}
