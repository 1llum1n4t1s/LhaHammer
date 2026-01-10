using LhaHammer.Models;

namespace LhaHammer.Services;

/// <summary>
/// Service for managing application configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from file
    /// </summary>
    Task<AppConfiguration> LoadConfigurationAsync();

    /// <summary>
    /// Saves configuration to file
    /// </summary>
    Task SaveConfigurationAsync(AppConfiguration config);

    /// <summary>
    /// Gets current configuration
    /// </summary>
    AppConfiguration GetConfiguration();

    /// <summary>
    /// Updates configuration
    /// </summary>
    void UpdateConfiguration(AppConfiguration config);

    /// <summary>
    /// Resets configuration to defaults
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Adds file to recent files list
    /// </summary>
    void AddRecentFile(string filePath);

    /// <summary>
    /// Gets recent files list
    /// </summary>
    List<string> GetRecentFiles();
}
