using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using LhaHammer.CommandLine;
using LhaHammer.Services;
using LhaHammer.ShellIntegration;
using LhaHammer.ViewModels;
using LhaHammer.Views;

namespace LhaHammer;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// メソッド: アプリケーション起動時の処理
    /// </summary>
    /// <param name="sender">パラメーター: イベント送信者</param>
    /// <param name="e">パラメーター: スタートアップイベント引数</param>
    private void OnApplicationStartup(object sender, StartupEventArgs e)
    {
        try
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Handle command-line arguments
            if (e.Args.Length > 0)
            {
                var commandLineHandler = new CommandLineHandler(
                    _serviceProvider.GetRequiredService<IArchiveService>(),
                    _serviceProvider.GetRequiredService<IConfigurationService>());

                _ = HandleCommandLineAsync(commandLineHandler, e.Args);
                return;
            }

            ShowMainWindow();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"アプリケーション起動エラー: {ex.Message}\n\n{ex.StackTrace}", "エラー");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// メソッド: コマンドライン引数を非同期で処理する
    /// </summary>
    /// <param name="commandLineHandler">パラメーター: コマンドラインハンドラー</param>
    /// <param name="args">パラメーター: コマンドライン引数</param>
    private async Task HandleCommandLineAsync(CommandLineHandler commandLineHandler, string[] args)
    {
        var exitCode = await commandLineHandler.ProcessCommandLineAsync(args);

        if (exitCode >= 0)
        {
            Environment.Exit(exitCode);
        }
        else
        {
            ShowMainWindow();
        }
    }

    /// <summary>
    /// メソッド: メインウィンドウを表示する
    /// </summary>
    private void ShowMainWindow()
    {
        var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
        if (mainWindow != null)
        {
            mainWindow.Show();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // メソッド: サービスを登録する
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
