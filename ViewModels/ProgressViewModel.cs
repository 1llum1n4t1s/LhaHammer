using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LhaHammer.Models;

namespace LhaHammer.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Operation in Progress";

    [ObservableProperty]
    private string _currentFile = string.Empty;

    [ObservableProperty]
    private double _percentComplete;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _processedFiles;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private long _processedBytes;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private bool _canCancel = true;

    private CancellationTokenSource? _cancellationTokenSource;

    public void UpdateProgress(OperationProgress progress)
    {
        CurrentFile = progress.CurrentFile;
        PercentComplete = progress.PercentComplete;
        StatusMessage = progress.StatusMessage;
        ProcessedFiles = progress.ProcessedFiles;
        TotalFiles = progress.TotalFiles;
        ProcessedBytes = progress.ProcessedBytes;
        TotalBytes = progress.TotalBytes;
        CanCancel = progress.CanCancel;
    }

    public void SetCancellationTokenSource(CancellationTokenSource cts)
    {
        _cancellationTokenSource = cts;
    }

    [RelayCommand]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling...";
        CanCancel = false;
    }
}
