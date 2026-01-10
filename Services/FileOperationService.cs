using System.Diagnostics;
using System.IO;

namespace LhaHammer.Services;

public class FileOperationService : IFileOperationService
{
    public async Task CopyFilesAsync(
        string[] sourcePaths,
        string destinationPath,
        IProgress<(string file, long bytesProcessed, long totalBytes)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var totalBytes = await GetTotalSizeAsync(sourcePaths);
        long processedBytes = 0;

        foreach (var sourcePath in sourcePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(sourcePath))
            {
                var destFile = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
                await CopyFileWithProgressAsync(sourcePath, destFile,
                    (current, total) => progress?.Report((sourcePath, processedBytes + current, totalBytes)),
                    cancellationToken);
                processedBytes += new FileInfo(sourcePath).Length;
            }
            else if (Directory.Exists(sourcePath))
            {
                var destDir = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
                await CopyDirectoryAsync(sourcePath, destDir,
                    (file, current, total) => progress?.Report((file, processedBytes + current, totalBytes)),
                    cancellationToken);
                processedBytes += await GetTotalSizeAsync(new[] { sourcePath });
            }
        }
    }

    public async Task MoveFilesAsync(
        string[] sourcePaths,
        string destinationPath,
        IProgress<(string file, long bytesProcessed, long totalBytes)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await CopyFilesAsync(sourcePaths, destinationPath, progress, cancellationToken);

        foreach (var sourcePath in sourcePaths)
        {
            if (File.Exists(sourcePath))
                File.Delete(sourcePath);
            else if (Directory.Exists(sourcePath))
                Directory.Delete(sourcePath, true);
        }
    }

    public async Task<bool> DeleteFilesAsync(string[] filePaths, bool confirm = true)
    {
        if (confirm)
        {
            // In a real implementation, show a confirmation dialog
            // For now, just proceed
        }

        await Task.Run(() =>
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    else if (Directory.Exists(filePath))
                        Directory.Delete(filePath, true);
                }
                catch
                {
                    // Log error but continue
                }
            }
        });

        return true;
    }

    public async Task<long> GetTotalSizeAsync(string[] paths)
    {
        return await Task.Run(() =>
        {
            long total = 0;
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    total += new FileInfo(path).Length;
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    total += dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                }
            }
            return total;
        });
    }

    public bool IsPathWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".test_{Guid.NewGuid()}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var counter = 1;

        while (true)
        {
            var newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
            if (!File.Exists(newPath))
                return newPath;
            counter++;
        }
    }

    public void OpenFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
    }

    public void OpenDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                UseShellExecute = true
            });
        }
    }

    private async Task CopyFileWithProgressAsync(
        string sourcePath,
        string destPath,
        Action<long, long>? progress,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 81920; // 80 KB buffer
        var fileInfo = new FileInfo(sourcePath);
        var totalBytes = fileInfo.Length;
        long copiedBytes = 0;

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);
        using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

        var buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            copiedBytes += bytesRead;
            progress?.Invoke(copiedBytes, totalBytes);
        }
    }

    private async Task CopyDirectoryAsync(
        string sourcePath,
        string destPath,
        Action<string, long, long>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destPath);

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            var destFile = Path.Combine(destPath, Path.GetFileName(file));
            await CopyFileWithProgressAsync(file, destFile,
                (current, total) => progress?.Invoke(file, current, total),
                cancellationToken);
        }

        foreach (var directory in Directory.GetDirectories(sourcePath))
        {
            var destDir = Path.Combine(destPath, Path.GetFileName(directory));
            await CopyDirectoryAsync(directory, destDir, progress, cancellationToken);
        }
    }
}
