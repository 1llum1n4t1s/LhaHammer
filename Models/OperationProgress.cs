namespace LhaHammer.Models;

/// <summary>
/// Progress information for archive operations
/// </summary>
public class OperationProgress
{
    public OperationType OperationType { get; init; }
    public string CurrentFile { get; set; } = string.Empty;
    public long ProcessedBytes { get; set; }
    public long TotalBytes { get; set; }
    public int ProcessedFiles { get; set; }
    public int TotalFiles { get; set; }
    public double PercentComplete => TotalBytes > 0 ? (double)ProcessedBytes / TotalBytes * 100 : 0;
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public long BytesPerSecond { get; set; }
    public bool CanCancel { get; init; } = true;
    public string StatusMessage { get; set; } = string.Empty;
}

public enum OperationType
{
    Compress,
    Extract,
    Test,
    Delete,
    Add,
    Update
}

/// <summary>
/// Result of an archive operation
/// </summary>
public class OperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public List<string> ProcessedFiles { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}
