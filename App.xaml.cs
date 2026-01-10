using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using LhaHammer.CommandLine;
using LhaHammer.Services;
using LhaHammer.ShellIntegration;
using LhaHammer.ViewModels;
using LhaHammer.Views;

namespace LhaHammer;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        // Handle command-line arguments
        if (e.Args.Length > 0)
        {
            var commandLineHandler = new CommandLineHandler(
                _serviceProvider.GetRequiredService<IArchiveService>(),
                _serviceProvider.GetRequiredService<IConfigurationService>());

            var exitCode = await commandLineHandler.ProcessCommandLineAsync(e.Args);

            if (exitCode >= 0)
            {
                // Command-line mode - exit with code
                Environment.Exit(exitCode);
                return;
            }
            // exitCode == -1 means launch GUI (file path provided or default)
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IArchiveService, ArchiveService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IFileOperationService, FileOperationService>();
        services.AddSingleton<ShellIntegrationService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ConfigurationViewModel>();
        services.AddTransient<ProgressViewModel>();
        services.AddTransient<ShellIntegrationViewModel>();

        // Register Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<ShellIntegrationWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
