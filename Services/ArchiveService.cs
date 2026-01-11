using System.IO;
using LhaHammer.Models;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;

namespace LhaHammer.Services;

public class ArchiveService : IArchiveService
{
    private static readonly Dictionary<ArchiveFormat, ArchiveFormatInfo> _formatInfos = new()
    {
        [ArchiveFormat.Zip] = new ArchiveFormatInfo
        {
            Format = ArchiveFormat.Zip,
            Name = "ZIP",
            Extension = ".zip",
            Extensions = new[] { ".zip", ".zipx" },
            Capabilities = FormatCapabilities.Read | FormatCapabilities.Write | FormatCapabilities.Test | FormatCapabilities.Encrypt,
            Description = "ZIP Archive",
            MimeType = "application/zip"
        },
        [ArchiveFormat.SevenZip] = new ArchiveFormatInfo
        {
            Format = ArchiveFormat.SevenZip,
            Name = "7-Zip",
            Extension = ".7z",
            Extensions = new[] { ".7z" },
            Capabilities = FormatCapabilities.Read | FormatCapabilities.Write | FormatCapabilities.Test | FormatCapabilities.Encrypt,
            Description = "7-Zip Archive",
            MimeType = "application/x-7z-compressed"
        },
        [ArchiveFormat.Tar] = new ArchiveFormatInfo
        {
            Format = ArchiveFormat.Tar,
            Name = "TAR",
            Extension = ".tar",
            Extensions = new[] { ".tar" },
            Capabilities = FormatCapabilities.Read | FormatCapabilities.Write | FormatCapabilities.Test,
            Description = "TAR Archive",
            MimeType = "application/x-tar"
        },
        [ArchiveFormat.GZip] = new ArchiveFormatInfo
        {
            Format = ArchiveFormat.GZip,
            Name = "GZIP",
            Extension = ".gz",
            Extensions = new[] { ".gz", ".tgz", ".tar.gz" },
            Capabilities = FormatCapabilities.Read | FormatCapabilities.Write | FormatCapabilities.Test,
            Description = "GZIP Compressed Archive",
            MimeType = "application/gzip"
        },
        [ArchiveFormat.Rar] = new ArchiveFormatInfo
        {
            Format = ArchiveFormat.Rar,
            Name = "RAR",
            Extension = ".rar",
            Extensions = new[] { ".rar" },
            Capabilities = FormatCapabilities.Read | FormatCapabilities.Test,
            Description = "RAR Archive",
            MimeType = "application/vnd.rar"
        }
    };

    public async Task<ArchiveMetadata> OpenArchiveAsync(string filePath, string? password = null)
    {
        return await Task.Run(() =>
        {
            using var archive = ArchiveFactory.Open(filePath, new ReaderOptions { Password = password });

            var format = DetectFormatFromArchive(archive);
            long totalSize = 0;
            long compressedSize = 0;
            int entryCount = 0;
            bool isEncrypted = false;

            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                totalSize += entry.Size;
                compressedSize += entry.CompressedSize;
                entryCount++;
                if (entry.IsEncrypted)
                    isEncrypted = true;
            }

            return new ArchiveMetadata
            {
                FilePath = filePath,
                Format = format,
                TotalSize = totalSize,
                CompressedSize = compressedSize,
                EntryCount = entryCount,
                IsEncrypted = isEncrypted,
                IsSolid = archive.IsSolid,
                IsMultiVolume = false
            };
        });
    }

    public async Task<List<ArchiveEntry>> ListEntriesAsync(string filePath, string? password = null)
    {
        return await Task.Run(() =>
        {
            using var archive = ArchiveFactory.Open(filePath, new ReaderOptions { Password = password });
            var entries = new List<ArchiveEntry>();

            foreach (var entry in archive.Entries)
            {
                entries.Add(new ArchiveEntry
                {
                    Path = entry.Key ?? string.Empty,
                    FileName = Path.GetFileName(entry.Key ?? string.Empty),
                    Size = entry.Size,
                    CompressedSize = entry.CompressedSize,
                    LastModified = entry.LastModifiedTime ?? DateTime.MinValue,
                    IsDirectory = entry.IsDirectory,
                    IsEncrypted = entry.IsEncrypted,
                    Crc32 = (uint)(entry.Crc),
                    CompressionMethod = entry.CompressionType.ToString()
                });
            }

            return entries;
        });
    }

    public async Task<OperationResult> ExtractAsync(
        string archivePath,
        string outputPath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        string? password = null,
        string[]? selectedFiles = null)
    {
        var errors = new List<string>();
        var processedFiles = new List<string>();
        var startTime = DateTime.Now;

        try
        {
            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.Open(archivePath, new ReaderOptions { Password = password });
                var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();

                if (selectedFiles != null && selectedFiles.Length > 0)
                {
                    entries = entries.Where(e => selectedFiles.Contains(e.Key)).ToList();
                }

                long totalBytes = entries.Sum(e => e.Size);
                long processedBytes = 0;
                int fileIndex = 0;

                foreach (var entry in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var destPath = Path.Combine(outputPath, entry.Key ?? string.Empty);
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                        entry.WriteToDirectory(outputPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true,
                            PreserveFileTime = true
                        });

                        processedFiles.Add(entry.Key ?? string.Empty);
                        processedBytes += entry.Size;
                        fileIndex++;

                        progress?.Report(new OperationProgress
                        {
                            OperationType = OperationType.Extract,
                            CurrentFile = entry.Key ?? string.Empty,
                            ProcessedBytes = processedBytes,
                            TotalBytes = totalBytes,
                            ProcessedFiles = fileIndex,
                            TotalFiles = entries.Count,
                            ElapsedTime = DateTime.Now - startTime,
                            StatusMessage = $"Extracting: {entry.Key}"
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{entry.Key}: {ex.Message}");
                    }
                }
            }, cancellationToken);

            return new OperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0 ? "Extraction completed successfully" : $"Extraction completed with {errors.Count} errors",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (OperationCanceledException)
        {
            return new OperationResult
            {
                Success = false,
                Message = "Extraction cancelled by user",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"Extraction failed: {ex.Message}",
                Exception = ex,
                Errors = errors
            };
        }
    }

    public async Task<OperationResult> CompressAsync(
        string[] sourcePaths,
        string archivePath,
        ArchiveFormat format,
        CompressionConfig config,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var processedFiles = new List<string>();
        var startTime = DateTime.Now;

        try
        {
            await Task.Run(() =>
            {
                var writerOptions = new WriterOptions(GetCompressionType(format))
                {
                    LeaveStreamOpen = false,
                    ArchiveEncoding = new ArchiveEncoding { Default = System.Text.Encoding.UTF8 }
                };

                using var stream = File.Create(archivePath);
                using var writer = WriterFactory.Open(stream, GetArchiveType(format), writerOptions);

                var allFiles = new List<string>();
                foreach (var sourcePath in sourcePaths)
                {
                    if (File.Exists(sourcePath))
                    {
                        allFiles.Add(sourcePath);
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        allFiles.AddRange(Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories));
                    }
                }

                long totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                long processedBytes = 0;

                foreach (var filePath in allFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var relativePath = GetRelativePath(sourcePaths[0], filePath);
                        writer.Write(relativePath, filePath);

                        processedFiles.Add(filePath);
                        processedBytes += new FileInfo(filePath).Length;

                        progress?.Report(new OperationProgress
                        {
                            OperationType = OperationType.Compress,
                            CurrentFile = filePath,
                            ProcessedBytes = processedBytes,
                            TotalBytes = totalBytes,
                            ProcessedFiles = processedFiles.Count,
                            TotalFiles = allFiles.Count,
                            ElapsedTime = DateTime.Now - startTime,
                            StatusMessage = $"Compressing: {Path.GetFileName(filePath)}"
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{filePath}: {ex.Message}");
                    }
                }
            }, cancellationToken);

            return new OperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0 ? "Compression completed successfully" : $"Compression completed with {errors.Count} errors",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"Compression failed: {ex.Message}",
                Exception = ex,
                Errors = errors
            };
        }
    }

    public async Task<OperationResult> TestArchiveAsync(
        string archivePath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        string? password = null)
    {
        var errors = new List<string>();
        var startTime = DateTime.Now;

        try
        {
            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.Open(archivePath, new ReaderOptions { Password = password });
                var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
                int processedCount = 0;

                foreach (var entry in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using var stream = entry.OpenEntryStream();
                        var buffer = new byte[8192];
                        while (stream.Read(buffer, 0, buffer.Length) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        processedCount++;
                        progress?.Report(new OperationProgress
                        {
                            OperationType = OperationType.Test,
                            CurrentFile = entry.Key ?? string.Empty,
                            ProcessedFiles = processedCount,
                            TotalFiles = entries.Count,
                            ElapsedTime = DateTime.Now - startTime,
                            StatusMessage = $"Testing: {entry.Key}"
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{entry.Key}: {ex.Message}");
                    }
                }
            }, cancellationToken);

            return new OperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0 ? "Archive test passed" : $"Archive test found {errors.Count} errors",
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"Archive test failed: {ex.Message}",
                Exception = ex,
                Errors = errors
            };
        }
    }

    public async Task<OperationResult> AddFilesAsync(
        string archivePath,
        string[] filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var processedFiles = new List<string>();
        var startTime = DateTime.Now;

        try
        {
            await Task.Run(() =>
            {
                var tempPath = archivePath + ".tmp";
                var format = DetectFormatAsync(archivePath).Result;

                try
                {
                    var writerOptions = new WriterOptions(GetCompressionType(format))
                    {
                        LeaveStreamOpen = false,
                        ArchiveEncoding = new ArchiveEncoding { Default = System.Text.Encoding.UTF8 }
                    };

                    using (var tempStream = File.Create(tempPath))
                    using (var writer = WriterFactory.Open(tempStream, GetArchiveType(format), writerOptions))
                    {
                        // Copy existing entries from the original archive
                        using (var archive = ArchiveFactory.Open(archivePath))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (!entry.IsDirectory && entry.Key != null)
                                {
                                    using (var entryStream = entry.OpenEntryStream())
                                    {
                                        writer.Write(entry.Key, entryStream);
                                    }
                                }
                            }
                        }

                        // Add new files
                        long totalBytes = filePaths.Sum(f => new FileInfo(f).Length);
                        long processedBytes = 0;

                        foreach (var filePath in filePaths)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                if (!File.Exists(filePath))
                                {
                                    errors.Add($"{filePath}: File not found");
                                    continue;
                                }

                                var relativePath = GetRelativePath(filePaths[0], filePath);
                                writer.Write(relativePath, filePath);

                                processedFiles.Add(filePath);
                                processedBytes += new FileInfo(filePath).Length;

                                progress?.Report(new OperationProgress
                                {
                                    OperationType = OperationType.Add,
                                    CurrentFile = filePath,
                                    ProcessedBytes = processedBytes,
                                    TotalBytes = totalBytes,
                                    ProcessedFiles = processedFiles.Count,
                                    TotalFiles = filePaths.Length,
                                    ElapsedTime = DateTime.Now - startTime,
                                    StatusMessage = $"Adding: {Path.GetFileName(filePath)}"
                                });
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"{filePath}: {ex.Message}");
                            }
                        }
                    }

                    // Replace original archive with updated one
                    File.Delete(archivePath);
                    File.Move(tempPath, archivePath);
                }
                catch
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    throw;
                }
            }, cancellationToken);

            return new OperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0 ? "Files added successfully" : $"Adding files completed with {errors.Count} errors",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (OperationCanceledException)
        {
            return new OperationResult
            {
                Success = false,
                Message = "Adding files cancelled by user",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"Adding files failed: {ex.Message}",
                Exception = ex,
                Errors = errors
            };
        }
    }

    public async Task<OperationResult> DeleteFilesAsync(
        string archivePath,
        string[] entryPaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var processedFiles = new List<string>();
        var startTime = DateTime.Now;
        var entriesToDelete = new HashSet<string>(entryPaths);

        try
        {
            await Task.Run(() =>
            {
                var tempPath = archivePath + ".tmp";
                var format = DetectFormatAsync(archivePath).Result;

                try
                {
                    var writerOptions = new WriterOptions(GetCompressionType(format))
                    {
                        LeaveStreamOpen = false,
                        ArchiveEncoding = new ArchiveEncoding { Default = System.Text.Encoding.UTF8 }
                    };

                    using (var tempStream = File.Create(tempPath))
                    using (var writer = WriterFactory.Open(tempStream, GetArchiveType(format), writerOptions))
                    {
                        // Copy entries that should not be deleted
                        using (var archive = ArchiveFactory.Open(archivePath))
                        {
                            var entries = archive.Entries.ToList();
                            int processedCount = 0;

                            foreach (var entry in entries)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (entry.Key == null)
                                    continue;

                                // Skip entries that should be deleted
                                if (entriesToDelete.Contains(entry.Key))
                                {
                                    processedFiles.Add(entry.Key);
                                    processedCount++;
                                    continue;
                                }

                                try
                                {
                                    if (!entry.IsDirectory && entry.Key != null)
                                    {
                                        using (var entryStream = entry.OpenEntryStream())
                                        {
                                            writer.Write(entry.Key, entryStream);
                                        }
                                    }

                                    processedCount++;
                                    progress?.Report(new OperationProgress
                                    {
                                        OperationType = OperationType.Delete,
                                        CurrentFile = entry.Key ?? string.Empty,
                                        ProcessedFiles = processedCount,
                                        TotalFiles = entries.Count,
                                        ElapsedTime = DateTime.Now - startTime,
                                        StatusMessage = $"Processing: {entry.Key}"
                                    });
                                }
                                catch (Exception ex)
                                {
                                    errors.Add($"{entry.Key}: {ex.Message}");
                                }
                            }
                        }
                    }

                    // Replace original archive with updated one
                    File.Delete(archivePath);
                    File.Move(tempPath, archivePath);
                }
                catch
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    throw;
                }
            }, cancellationToken);

            return new OperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0 ? "Files deleted successfully" : $"Deleting files completed with {errors.Count} errors",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (OperationCanceledException)
        {
            return new OperationResult
            {
                Success = false,
                Message = "Deleting files cancelled by user",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"Deleting files failed: {ex.Message}",
                Exception = ex,
                Errors = errors
            };
        }
    }

    public async Task<ArchiveFormat> DetectFormatAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".zip" or ".zipx" => ArchiveFormat.Zip,
                ".7z" => ArchiveFormat.SevenZip,
                ".tar" => ArchiveFormat.Tar,
                ".gz" or ".tgz" => ArchiveFormat.GZip,
                ".bz2" or ".tbz2" => ArchiveFormat.BZip2,
                ".xz" => ArchiveFormat.Xz,
                ".lzma" => ArchiveFormat.Lzma,
                ".rar" => ArchiveFormat.Rar,
                ".lzh" or ".lha" => ArchiveFormat.Lzh,
                ".cab" => ArchiveFormat.Cab,
                ".iso" => ArchiveFormat.Iso,
                _ => ArchiveFormat.Unknown
            };
        });
    }

    public List<ArchiveFormatInfo> GetSupportedFormats()
    {
        return _formatInfos.Values.ToList();
    }

    public ArchiveFormatInfo? GetFormatInfo(ArchiveFormat format)
    {
        return _formatInfos.TryGetValue(format, out var info) ? info : null;
    }

    private static ArchiveFormat DetectFormatFromArchive(IArchive archive)
    {
        return archive.Type switch
        {
            SharpCompress.Common.ArchiveType.Zip => ArchiveFormat.Zip,
            SharpCompress.Common.ArchiveType.SevenZip => ArchiveFormat.SevenZip,
            SharpCompress.Common.ArchiveType.Tar => ArchiveFormat.Tar,
            SharpCompress.Common.ArchiveType.GZip => ArchiveFormat.GZip,
            SharpCompress.Common.ArchiveType.Rar => ArchiveFormat.Rar,
            _ => ArchiveFormat.Unknown
        };
    }

    private static CompressionType GetCompressionType(ArchiveFormat format)
    {
        return format switch
        {
            ArchiveFormat.GZip => CompressionType.GZip,
            ArchiveFormat.BZip2 => CompressionType.BZip2,
            ArchiveFormat.Lzma => CompressionType.LZMA,
            ArchiveFormat.Xz => CompressionType.Xz,
            _ => CompressionType.Deflate
        };
    }

    private static SharpCompress.Common.ArchiveType GetArchiveType(ArchiveFormat format)
    {
        return format switch
        {
            ArchiveFormat.Zip => SharpCompress.Common.ArchiveType.Zip,
            ArchiveFormat.SevenZip => SharpCompress.Common.ArchiveType.SevenZip,
            ArchiveFormat.Tar => SharpCompress.Common.ArchiveType.Tar,
            ArchiveFormat.GZip => SharpCompress.Common.ArchiveType.GZip,
            _ => SharpCompress.Common.ArchiveType.Zip
        };
    }

    private static string GetRelativePath(string basePath, string fullPath)
    {
        if (File.Exists(basePath))
        {
            basePath = Path.GetDirectoryName(basePath)!;
        }

        var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
        var fullUri = new Uri(fullPath);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }
}
