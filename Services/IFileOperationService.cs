namespace LhaHammer.Services;

/// <summary>
/// Service for file system operations
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// Copies files with progress reporting
    /// </summary>
    Task CopyFilesAsync(
        string[] sourcePaths,
        string destinationPath,
        IProgress<(string file, long bytesProcessed, long totalBytes)>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves files with progress reporting
    /// </summary>
    Task MoveFilesAsync(
        string[] sourcePaths,
        string destinationPath,
        IProgress<(string file, long bytesProcessed, long totalBytes)>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes files with confirmation
    /// </summary>
    Task<bool> DeleteFilesAsync(string[] filePaths, bool confirm = true);

    /// <summary>
    /// Gets total size of files/directories
    /// </summary>
    Task<long> GetTotalSizeAsync(string[] paths);

    /// <summary>
    /// Validates if path is writable
    /// </summary>
    bool IsPathWritable(string path);

    /// <summary>
    /// Creates unique filename if file exists
    /// </summary>
    string GetUniqueFilePath(string filePath);

    /// <summary>
    /// Opens file in default application
    /// </summary>
    void OpenFile(string filePath);

    /// <summary>
    /// Opens directory in explorer
    /// </summary>
    void OpenDirectory(string directoryPath);
}
