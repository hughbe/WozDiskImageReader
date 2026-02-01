namespace WozDiskImageReader.Chunks;

/// <summary>
/// Specifies the boot sector format used in the WOZ disk image.
/// </summary>
public enum BootSectorFormat : byte
{
    /// <summary>
    /// Unknown or unspecified boot sector type.
    /// </summary>
    Unknown = 0x00,

    /// <summary>
    /// 16-sector boot sector format.
    /// </summary>
    Sixteen = 0x01,

    /// <summary>
    /// 13-sector boot sector format.
    /// </summary>
    Thirteen = 0x02,

    /// <summary>
    /// Both 13 and 16 sector formats are supported.
    /// </summary>
    Both = 0x03,
}
