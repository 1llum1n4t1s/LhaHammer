using System.Windows;
using LhaHammer.ViewModels;

namespace LhaHammer.Views;

public partial class ShellIntegrationWindow : Window
{
    public ShellIntegrationWindow(ShellIntegrationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
