namespace LhaHammer.Models;

/// <summary>
/// Represents a single entry (file or directory) in an archive
/// </summary>
public class ArchiveEntry
{
    public required string Path { get; init; }
    public required string FileName { get; init; }
    public required long Size { get; init; }
    public required long CompressedSize { get; init; }
    public required DateTime LastModified { get; init; }
    public required bool IsDirectory { get; init; }
    public required bool IsEncrypted { get; init; }
    public uint Crc32 { get; init; }
    public string CompressionMethod { get; init; } = string.Empty;
    public int CompressionRatio => Size > 0 ? (int)((1.0 - (double)CompressedSize / Size) * 100) : 0;
    public string Attributes { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;

    public string GetDirectory()
    {
        var lastSlash = Path.LastIndexOf('/');
        return lastSlash >= 0 ? Path[..lastSlash] : string.Empty;
    }
}

/// <summary>
/// Archive metadata
/// </summary>
public class ArchiveMetadata
{
    public required string FilePath { get; init; }
    public required ArchiveFormat Format { get; init; }
    public required long TotalSize { get; init; }
    public required long CompressedSize { get; init; }
    public required int EntryCount { get; init; }
    public required bool IsEncrypted { get; init; }
    public required bool IsSolid { get; init; }
    public required bool IsMultiVolume { get; init; }
    public string Comment { get; init; } = string.Empty;
    public DateTime? CreatedDate { get; init; }
}
