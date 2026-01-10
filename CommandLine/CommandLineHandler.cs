using LhaHammer.Models;
using LhaHammer.Services;

namespace LhaHammer.CommandLine;

/// <summary>
/// Handles command-line arguments for LhaHammer
/// </summary>
public class CommandLineHandler
{
    private readonly IArchiveService _archiveService;
    private readonly IConfigurationService _configService;

    public CommandLineHandler(IArchiveService archiveService, IConfigurationService configService)
    {
        _archiveService = archiveService;
        _configService = configService;
    }

    public async Task<int> ProcessCommandLineAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // No arguments, launch GUI
            return -1;
        }

        try
        {
            var command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "extract":
                case "e":
                case "x":
                    return await ExtractCommandAsync(args);

                case "compress":
                case "c":
                case "a":
                    return await CompressCommandAsync(args);

                case "list":
                case "l":
                    return await ListCommandAsync(args);

                case "test":
                case "t":
                    return await TestCommandAsync(args);

                case "help":
                case "--help":
                case "-h":
                case "/?":
                    ShowHelp();
                    return 0;

                default:
                    // If first argument is a file path, try to open it in GUI
                    if (File.Exists(args[0]))
                    {
                        return -1; // Launch GUI with file
                    }
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Use 'LhaHammer --help' for usage information.");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> ExtractCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: LhaHammer extract <archive> [output_directory] [options]");
            return 1;
        }

        var archivePath = args[1];
        var outputPath = args.Length > 2 ? args[2] : Path.GetDirectoryName(archivePath) ?? ".";

        if (!File.Exists(archivePath))
        {
            Console.WriteLine($"Error: Archive not found: {archivePath}");
            return 1;
        }

        Console.WriteLine($"Extracting: {archivePath}");
        Console.WriteLine($"Output: {outputPath}");

        var progress = new Progress<OperationProgress>(p =>
        {
            Console.Write($"\r{p.CurrentFile} - {p.PercentComplete:F1}%");
        });

        var result = await _archiveService.ExtractAsync(archivePath, outputPath, progress);

        Console.WriteLine();
        if (result.Success)
        {
            Console.WriteLine($"Successfully extracted {result.ProcessedFiles.Count} files");
            return 0;
        }
        else
        {
            Console.WriteLine($"Extraction failed: {result.Message}");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  Error: {error}");
            }
            return 1;
        }
    }

    private async Task<int> CompressCommandAsync(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: LhaHammer compress <archive> <files...> [options]");
            return 1;
        }

        var archivePath = args[1];
        var sourcePaths = args.Skip(2).ToArray();

        var format = await _archiveService.DetectFormatAsync(archivePath);
        if (format == ArchiveFormat.Unknown)
        {
            Console.WriteLine("Error: Could not determine archive format from extension");
            return 1;
        }

        Console.WriteLine($"Creating archive: {archivePath}");
        Console.WriteLine($"Format: {format}");
        Console.WriteLine($"Files: {sourcePaths.Length}");

        var config = _configService.GetConfiguration().Compression;
        var progress = new Progress<OperationProgress>(p =>
        {
            Console.Write($"\r{p.CurrentFile} - {p.PercentComplete:F1}%");
        });

        var result = await _archiveService.CompressAsync(sourcePaths, archivePath, format, config, progress);

        Console.WriteLine();
        if (result.Success)
        {
            Console.WriteLine($"Successfully compressed {result.ProcessedFiles.Count} files");
            return 0;
        }
        else
        {
            Console.WriteLine($"Compression failed: {result.Message}");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  Error: {error}");
            }
            return 1;
        }
    }

    private async Task<int> ListCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: LhaHammer list <archive>");
            return 1;
        }

        var archivePath = args[1];

        if (!File.Exists(archivePath))
        {
            Console.WriteLine($"Error: Archive not found: {archivePath}");
            return 1;
        }

        try
        {
            var metadata = await _archiveService.OpenArchiveAsync(archivePath);
            var entries = await _archiveService.ListEntriesAsync(archivePath);

            Console.WriteLine($"Archive: {Path.GetFileName(archivePath)}");
            Console.WriteLine($"Format: {metadata.Format}");
            Console.WriteLine($"Entries: {metadata.EntryCount}");
            Console.WriteLine($"Total Size: {metadata.TotalSize:N0} bytes");
            Console.WriteLine($"Compressed: {metadata.CompressedSize:N0} bytes");
            Console.WriteLine($"Ratio: {(1.0 - (double)metadata.CompressedSize / metadata.TotalSize) * 100:F1}%");
            Console.WriteLine();
            Console.WriteLine("Files:");
            Console.WriteLine("{0,-50} {1,15} {2,15} {3,6}", "Name", "Size", "Compressed", "Ratio");
            Console.WriteLine(new string('-', 90));

            foreach (var entry in entries.Where(e => !e.IsDirectory))
            {
                Console.WriteLine("{0,-50} {1,15:N0} {2,15:N0} {3,6}%",
                    entry.FileName.Length > 50 ? entry.FileName.Substring(0, 47) + "..." : entry.FileName,
                    entry.Size,
                    entry.CompressedSize,
                    entry.CompressionRatio);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> TestCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: LhaHammer test <archive>");
            return 1;
        }

        var archivePath = args[1];

        if (!File.Exists(archivePath))
        {
            Console.WriteLine($"Error: Archive not found: {archivePath}");
            return 1;
        }

        Console.WriteLine($"Testing: {archivePath}");

        var progress = new Progress<OperationProgress>(p =>
        {
            Console.Write($"\r{p.CurrentFile}");
        });

        var result = await _archiveService.TestArchiveAsync(archivePath, progress);

        Console.WriteLine();
        if (result.Success)
        {
            Console.WriteLine("Archive test passed - no errors detected");
            return 0;
        }
        else
        {
            Console.WriteLine($"Archive test failed: {result.Message}");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  Error: {error}");
            }
            return 1;
        }
    }

    private void ShowHelp()
    {
        Console.WriteLine(@"
LhaHammer - Multi-format Archive Manager
Copyright © ゆろち 2025

Usage:
  LhaHammer [command] [options]

Commands:
  extract (e, x)    Extract archive
    LhaHammer extract <archive> [output_directory]

  compress (c, a)   Create archive
    LhaHammer compress <archive> <files...>

  list (l)          List archive contents
    LhaHammer list <archive>

  test (t)          Test archive integrity
    LhaHammer test <archive>

  help              Show this help message

Supported Formats:
  Read/Write: ZIP, 7z, TAR, GZIP, BZ2, LZMA, XZ
  Read-Only:  RAR, LZH, CAB, ISO

Examples:
  LhaHammer extract archive.zip
  LhaHammer extract archive.zip C:\output
  LhaHammer compress backup.zip file1.txt file2.txt folder/
  LhaHammer list archive.7z
  LhaHammer test archive.rar

If no command is provided, the GUI will launch.
");
    }
}
