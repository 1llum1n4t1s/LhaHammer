namespace LhaHammer.Models;

/// <summary>
/// Application configuration
/// </summary>
public class AppConfiguration
{
    public GeneralConfig General { get; set; } = new();
    public CompressionConfig Compression { get; set; } = new();
    public ExtractionConfig Extraction { get; set; } = new();
    public ShellExtensionConfig ShellExtension { get; set; } = new();
    public Dictionary<ArchiveFormat, FormatSpecificConfig> FormatConfigs { get; set; } = new();
}

public class GeneralConfig
{
    public string Language { get; set; } = "ja-JP";
    public string TempDirectory { get; set; } = Path.GetTempPath();
    public bool ConfirmDelete { get; set; } = true;
    public bool ShowHiddenFiles { get; set; } = false;
    public int MaxRecentFiles { get; set; } = 10;
    public List<string> RecentFiles { get; set; } = new();
    public bool CheckForUpdates { get; set; } = true;
}

public class CompressionConfig
{
    public ArchiveFormat DefaultFormat { get; set; } = ArchiveFormat.Zip;
    public int CompressionLevel { get; set; } = 5;
    public bool DeleteAfterCompression { get; set; } = false;
    public bool CreateSolidArchive { get; set; } = false;
    public long SplitVolumeSize { get; set; } = 0;
    public bool EncryptFileNames { get; set; } = false;
    public string DefaultPassword { get; set; } = string.Empty;
    public bool StoreRelativePaths { get; set; } = true;
}

public class ExtractionConfig
{
    public string DefaultOutputDirectory { get; set; } = string.Empty;
    public bool CreateSubfolder { get; set; } = true;
    public bool DeleteAfterExtraction { get; set; } = false;
    public bool PreserveTimestamps { get; set; } = true;
    public bool OverwriteWithoutPrompt { get; set; } = false;
    public bool SkipExistingFiles { get; set; } = false;
}

public class ShellExtensionConfig
{
    public bool EnableContextMenu { get; set; } = true;
    public bool EnableDragDrop { get; set; } = true;
    public List<string> AssociatedExtensions { get; set; } = new();
}

public class FormatSpecificConfig
{
    public ArchiveFormat Format { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}
