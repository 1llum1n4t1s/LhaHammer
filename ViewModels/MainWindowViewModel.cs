using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LhaHammer.Models;
using LhaHammer.Services;
using Microsoft.Win32;

namespace LhaHammer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IConfigurationService _configService;
    private readonly IFileOperationService _fileService;

    [ObservableProperty]
    private string _currentArchivePath = string.Empty;

    [ObservableProperty]
    private ArchiveMetadata? _currentArchive;

    [ObservableProperty]
    private ObservableCollection<ArchiveEntry> _entries = new();

    [ObservableProperty]
    private ObservableCollection<ArchiveEntry> _selectedEntries = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<string> RecentFiles { get; } = new();

    public MainWindowViewModel(
        IArchiveService archiveService,
        IConfigurationService configService,
        IFileOperationService fileService)
    {
        _archiveService = archiveService;
        _configService = configService;
        _fileService = fileService;

        LoadRecentFiles();
    }

    [RelayCommand]
    private async Task OpenArchiveAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "All Archives|*.zip;*.7z;*.rar;*.tar;*.gz;*.bz2;*.xz;*.lzh|" +
                    "ZIP Archives|*.zip|" +
                    "7-Zip Archives|*.7z|" +
                    "RAR Archives|*.rar|" +
                    "TAR Archives|*.tar|" +
                    "All Files|*.*",
            Title = "Open Archive"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadArchiveAsync(dialog.FileName);
        }
    }

    [RelayCommand]
    private async Task CreateArchiveAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP Archive|*.zip|7-Zip Archive|*.7z|TAR Archive|*.tar",
            Title = "Create Archive",
            DefaultExt = ".zip"
        };

        if (dialog.ShowDialog() == true)
        {
            // Show file selection dialog and create archive
            var fileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select files to compress"
            };

            if (fileDialog.ShowDialog() == true)
            {
                await CompressFilesAsync(fileDialog.FileNames, dialog.FileName);
            }
        }
    }

    [RelayCommand]
    private async Task ExtractSelectedAsync()
    {
        if (string.IsNullOrEmpty(CurrentArchivePath) || SelectedEntries.Count == 0)
            return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Output Directory",
            FileName = "Select Folder"
        };

        // Workaround to select folder
        var folderBrowser = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select extraction directory",
            ShowNewFolderButton = true
        };

        if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            await ExtractArchiveAsync(folderBrowser.SelectedPath,
                SelectedEntries.Select(e => e.Path).ToArray());
        }
    }

    [RelayCommand]
    private async Task ExtractAllAsync()
    {
        if (string.IsNullOrEmpty(CurrentArchivePath))
            return;

        var folderBrowser = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select extraction directory",
            ShowNewFolderButton = true
        };

        if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            await ExtractArchiveAsync(folderBrowser.SelectedPath);
        }
    }

    [RelayCommand]
    private async Task TestArchiveAsync()
    {
        if (string.IsNullOrEmpty(CurrentArchivePath))
            return;

        IsBusy = true;
        StatusMessage = "Testing archive...";

        try
        {
            var progress = new Progress<OperationProgress>(p =>
            {
                StatusMessage = p.StatusMessage;
            });

            var result = await _archiveService.TestArchiveAsync(CurrentArchivePath, progress);

            StatusMessage = result.Success ? "Archive test passed" : $"Archive test failed: {result.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // Open settings window
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    private async Task LoadArchiveAsync(string filePath)
    {
        IsBusy = true;
        StatusMessage = $"Loading {Path.GetFileName(filePath)}...";

        try
        {
            CurrentArchivePath = filePath;
            CurrentArchive = await _archiveService.OpenArchiveAsync(filePath);
            var entries = await _archiveService.ListEntriesAsync(filePath);

            Entries.Clear();
            foreach (var entry in entries)
            {
                Entries.Add(entry);
            }

            _configService.AddRecentFile(filePath);
            LoadRecentFiles();

            StatusMessage = $"Loaded {entries.Count} entries from {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading archive: {ex.Message}";
            CurrentArchivePath = string.Empty;
            CurrentArchive = null;
            Entries.Clear();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExtractArchiveAsync(string outputPath, string[]? selectedFiles = null)
    {
        IsBusy = true;

        try
        {
            var progress = new Progress<OperationProgress>(p =>
            {
                StatusMessage = $"Extracting: {p.CurrentFile} ({p.PercentComplete:F1}%)";
            });

            var result = await _archiveService.ExtractAsync(
                CurrentArchivePath,
                outputPath,
                progress,
                selectedFiles: selectedFiles);

            StatusMessage = result.Success
                ? $"Extracted {result.ProcessedFiles.Count} files successfully"
                : $"Extraction completed with errors: {result.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error extracting: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CompressFilesAsync(string[] sourcePaths, string archivePath)
    {
        IsBusy = true;

        try
        {
            var format = await _archiveService.DetectFormatAsync(archivePath);
            var config = _configService.GetConfiguration().Compression;

            var progress = new Progress<OperationProgress>(p =>
            {
                StatusMessage = $"Compressing: {p.CurrentFile} ({p.PercentComplete:F1}%)";
            });

            var result = await _archiveService.CompressAsync(
                sourcePaths,
                archivePath,
                format,
                config,
                progress);

            StatusMessage = result.Success
                ? $"Compressed {result.ProcessedFiles.Count} files successfully"
                : $"Compression completed with errors: {result.Message}";

            if (result.Success)
            {
                await LoadArchiveAsync(archivePath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error compressing: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var file in _configService.GetRecentFiles())
        {
            RecentFiles.Add(file);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (CurrentArchive == null)
            return;

        // Filter entries based on search text
        // This is a simple implementation - can be enhanced
    }
}
