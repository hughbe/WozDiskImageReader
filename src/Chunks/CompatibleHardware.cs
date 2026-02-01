namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents compatible hardware flags for a WOZ disk image.
/// </summary>
[Flags]
public enum CompatibleHardware : ushort
{
    /// <summary>
    /// Unknown compatibility.
    /// </summary>
    Unknown = 0x0000,

    /// <summary>
    /// Apple II.
    /// </summary>
    AppleII = 0x0001,

    /// <summary>
    /// Apple II Plus.
    /// </summary>
    AppleIIPlus = 0x0002,

    /// <summary>
    /// Apple IIe (unenhanced).
    /// </summary>
    AppleIIeUnenhanced = 0x0004,

    /// <summary>
    /// Apple IIc.
    /// </summary>
    AppleIIc = 0x0008,

    /// <summary>
    /// Apple IIe Enhanced.
    /// </summary>
    AppleIIeEnhanced = 0x0010,

    /// <summary>
    /// Apple IIgs.
    /// </summary>
    AppleIIgs = 0x0020,

    /// <summary>
    /// Apple IIc Plus.
    /// </summary>
    AppleIIcPlus = 0x0040,

    /// <summary>
    /// Apple III.
    /// </summary>
    AppleIII = 0x0080,

    /// <summary>
    /// Apple III Plus.
    /// </summary>
    AppleIIIPlus = 0x0100,
}
