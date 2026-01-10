using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LhaHammer.Models;
using LhaHammer.Services;

namespace LhaHammer.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;

    [ObservableProperty]
    private AppConfiguration _configuration;

    public ConfigurationViewModel(IConfigurationService configService)
    {
        _configService = configService;
        _configuration = configService.GetConfiguration();
    }

    [RelayCommand]
    private void Save()
    {
        _configService.UpdateConfiguration(Configuration);
    }

    [RelayCommand]
    private void Cancel()
    {
        Configuration = _configService.GetConfiguration();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _configService.ResetToDefaults();
        Configuration = _configService.GetConfiguration();
    }
}
