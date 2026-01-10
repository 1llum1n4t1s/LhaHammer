namespace LhaHammer.Models;

/// <summary>
/// Supported archive formats
/// </summary>
public enum ArchiveFormat
{
    Unknown,
    Zip,
    SevenZip,
    Tar,
    GZip,
    BZip2,
    Lzma,
    Xz,
    Zstd,
    Lz4,
    Lzh,
    Rar,
    Cab,
    Iso,
    Arj,
    Cpio,
    Z,
    Uuencode,
    Bza,
    Gza
}

/// <summary>
/// Archive format capabilities
/// </summary>
[Flags]
public enum FormatCapabilities
{
    None = 0,
    Read = 1,
    Write = 2,
    Test = 4,
    Encrypt = 8,
    MultiVolume = 16
}

/// <summary>
/// Archive format information
/// </summary>
public class ArchiveFormatInfo
{
    public required ArchiveFormat Format { get; init; }
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required string[] Extensions { get; init; }
    public required FormatCapabilities Capabilities { get; init; }
    public string Description { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;

    public bool CanRead => (Capabilities & FormatCapabilities.Read) != 0;
    public bool CanWrite => (Capabilities & FormatCapabilities.Write) != 0;
    public bool CanTest => (Capabilities & FormatCapabilities.Test) != 0;
    public bool CanEncrypt => (Capabilities & FormatCapabilities.Encrypt) != 0;
}
