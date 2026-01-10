using System.IO;
using LhaHammer.Models;
using Newtonsoft.Json;

namespace LhaHammer.Services;

public class ConfigurationService : IConfigurationService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LhaHammer");

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    private AppConfiguration _currentConfig;

    public ConfigurationService()
    {
        _currentConfig = new AppConfiguration();
        LoadConfigurationAsync().Wait();
    }

    public async Task<AppConfiguration> LoadConfigurationAsync()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = await File.ReadAllTextAsync(ConfigFilePath);
                _currentConfig = JsonConvert.DeserializeObject<AppConfiguration>(json) ?? new AppConfiguration();
            }
            else
            {
                _currentConfig = new AppConfiguration();
                await SaveConfigurationAsync(_currentConfig);
            }
        }
        catch
        {
            _currentConfig = new AppConfiguration();
        }

        return _currentConfig;
    }

    public async Task SaveConfigurationAsync(AppConfiguration config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            await File.WriteAllTextAsync(ConfigFilePath, json);
            _currentConfig = config;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    public AppConfiguration GetConfiguration()
    {
        return _currentConfig;
    }

    public void UpdateConfiguration(AppConfiguration config)
    {
        _currentConfig = config;
        SaveConfigurationAsync(config).Wait();
    }

    public void ResetToDefaults()
    {
        _currentConfig = new AppConfiguration();
        SaveConfigurationAsync(_currentConfig).Wait();
    }

    public void AddRecentFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var recent = _currentConfig.General.RecentFiles;
        recent.Remove(filePath);
        recent.Insert(0, filePath);

        while (recent.Count > _currentConfig.General.MaxRecentFiles)
        {
            recent.RemoveAt(recent.Count - 1);
        }

        SaveConfigurationAsync(_currentConfig).Wait();
    }

    public List<string> GetRecentFiles()
    {
        return _currentConfig.General.RecentFiles
            .Where(File.Exists)
            .ToList();
    }
}
