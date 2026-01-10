using System.Windows;
using LhaHammer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LhaHammer.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;
    }

    private void ShellIntegrationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var shellIntegrationWindow = _serviceProvider.GetRequiredService<ShellIntegrationWindow>();
        shellIntegrationWindow.Owner = this;
        shellIntegrationWindow.ShowDialog();
    }
}
