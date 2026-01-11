using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LhaHammer.Models;
using LhaHammer.ShellIntegration;

using MessageBox = System.Windows.MessageBox;

namespace LhaHammer.ViewModels;

public partial class ShellIntegrationViewModel : ObservableObject
{
    private readonly ShellIntegrationService _shellIntegrationService;

    [ObservableProperty]
    private bool _isShellExtensionEnabled;

    [ObservableProperty]
    private bool _isAdministrator;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ArchiveFormatAssociation> _fileAssociations = new();

    public ShellIntegrationViewModel(ShellIntegrationService shellIntegrationService)
    {
        _shellIntegrationService = shellIntegrationService;
        _isAdministrator = RegistryHelper.IsAdministrator();

        LoadCurrentStatus();
        LoadFileAssociations();
    }

    [RelayCommand]
    private void EnableShellExtension()
    {
        if (!IsAdministrator)
        {
            if (MessageBox.Show(
                "Administrator privileges are required to enable shell integration.\n\nDo you want to restart as administrator?",
                "Administrator Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    RegistryHelper.RestartAsAdministrator();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to restart as administrator: {ex.Message}";
                }
            }
            return;
        }

        try
        {
            _shellIntegrationService.RegisterShellExtension();
            IsShellExtensionEnabled = true;
            StatusMessage = "Shell extension enabled successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to enable shell extension: {ex.Message}";
            MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DisableShellExtension()
    {
        if (!IsAdministrator)
        {
            MessageBox.Show(
                "Administrator privileges are required to disable shell integration.",
                "Administrator Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            _shellIntegrationService.UnregisterShellExtension();
            IsShellExtensionEnabled = false;
            StatusMessage = "Shell extension disabled successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to disable shell extension: {ex.Message}";
            MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ApplyFileAssociations()
    {
        if (!IsAdministrator)
        {
            if (MessageBox.Show(
                "Administrator privileges are required to manage file associations.\n\nDo you want to restart as administrator?",
                "Administrator Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    RegistryHelper.RestartAsAdministrator();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to restart as administrator: {ex.Message}";
                }
            }
            return;
        }

        try
        {
            var manager = _shellIntegrationService.GetFileAssociationManager();
            var selectedFormats = FileAssociations.Where(a => a.IsAssociated).Select(a => a.Format);

            manager.RegisterFileAssociations(selectedFormats);

            StatusMessage = "File associations updated successfully";
            MessageBox.Show(StatusMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update file associations: {ex.Message}";
            MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RemoveFileAssociations()
    {
        if (!IsAdministrator)
        {
            MessageBox.Show(
                "Administrator privileges are required to manage file associations.",
                "Administrator Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
            "Are you sure you want to remove all file associations?",
            "Confirm",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var manager = _shellIntegrationService.GetFileAssociationManager();
            var allFormats = FileAssociations.Select(a => a.Format);

            manager.UnregisterFileAssociations(allFormats);

            foreach (var association in FileAssociations)
            {
                association.IsAssociated = false;
            }

            StatusMessage = "File associations removed successfully";
            MessageBox.Show(StatusMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to remove file associations: {ex.Message}";
            MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadCurrentStatus()
    {
        try
        {
            IsShellExtensionEnabled = _shellIntegrationService.IsShellExtensionRegistered();
            StatusMessage = IsShellExtensionEnabled
                ? "Shell extension is currently enabled"
                : "Shell extension is currently disabled";
        }
        catch
        {
            IsShellExtensionEnabled = false;
            StatusMessage = "Unable to determine shell extension status";
        }
    }

    private void LoadFileAssociations()
    {
        var manager = _shellIntegrationService.GetFileAssociationManager();
        var formats = new[]
        {
            ArchiveFormat.Zip,
            ArchiveFormat.SevenZip,
            ArchiveFormat.Tar,
            ArchiveFormat.GZip,
            ArchiveFormat.BZip2,
            ArchiveFormat.Xz,
            ArchiveFormat.Rar,
            ArchiveFormat.Lzh,
            ArchiveFormat.Cab,
            ArchiveFormat.Iso
        };

        foreach (var format in formats)
        {
            var extensions = FileAssociationManager.GetExtensions(format);
            var isAssociated = manager.AreFileAssociationsRegistered(new[] { format });

            FileAssociations.Add(new ArchiveFormatAssociation
            {
                Format = format,
                FormatName = format.ToString(),
                Extensions = string.Join(", ", extensions),
                IsAssociated = isAssociated
            });
        }
    }
}

public partial class ArchiveFormatAssociation : ObservableObject
{
    public required ArchiveFormat Format { get; init; }
    public required string FormatName { get; init; }
    public required string Extensions { get; init; }

    [ObservableProperty]
    private bool _isAssociated;
}
