namespace AegisEInvoicing.Interswitch.Models.Enumerators;

/// <summary>
/// Strategy for how the 32-byte AES key is formed from the concatenated string.
/// </summary>
public enum KeyMaterialStrategy
{
    /// <summary>
    /// Use raw UTF-8 bytes. If length != 32, throw.
    /// </summary>
    RawStrict = 0,

    /// <summary>
    /// Always hash the UTF-8 string with SHA-256 to 32 bytes (safe, deterministic).
    /// </summary>
    Sha256 = 1
}