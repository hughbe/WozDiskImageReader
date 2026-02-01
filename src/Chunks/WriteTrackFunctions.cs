namespace WozDiskImageReader.Chunks;

/// <summary>
/// Represents additional write functions for a track write operation.
/// Bit field with a 1 indicating to perform the action.
/// </summary>
[Flags]
public enum WriteTrackFunctions : byte
{
    /// <summary>
    /// No additional functions.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Wipe the track before writing.
    /// </summary>
    WipeTrackBeforeWriting = 0x01,
}
