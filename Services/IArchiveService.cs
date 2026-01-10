using LhaHammer.Models;

namespace LhaHammer.Services;

/// <summary>
/// Service for archive operations
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Opens an archive and returns its metadata
    /// </summary>
    Task<ArchiveMetadata> OpenArchiveAsync(string filePath, string? password = null);

    /// <summary>
    /// Lists all entries in an archive
    /// </summary>
    Task<List<ArchiveEntry>> ListEntriesAsync(string filePath, string? password = null);

    /// <summary>
    /// Extracts archive to specified directory
    /// </summary>
    Task<OperationResult> ExtractAsync(
        string archivePath,
        string outputPath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        string? password = null,
        string[]? selectedFiles = null);

    /// <summary>
    /// Creates a new archive from files/directories
    /// </summary>
    Task<OperationResult> CompressAsync(
        string[] sourcePaths,
        string archivePath,
        ArchiveFormat format,
        CompressionConfig config,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests archive integrity
    /// </summary>
    Task<OperationResult> TestArchiveAsync(
        string archivePath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        string? password = null);

    /// <summary>
    /// Adds files to existing archive
    /// </summary>
    Task<OperationResult> AddFilesAsync(
        string archivePath,
        string[] filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes files from archive
    /// </summary>
    Task<OperationResult> DeleteFilesAsync(
        string archivePath,
        string[] entryPaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects archive format from file
    /// </summary>
    Task<ArchiveFormat> DetectFormatAsync(string filePath);

    /// <summary>
    /// Gets all supported archive formats
    /// </summary>
    List<ArchiveFormatInfo> GetSupportedFormats();

    /// <summary>
    /// Gets format information for specific format
    /// </summary>
    ArchiveFormatInfo? GetFormatInfo(ArchiveFormat format);
}
