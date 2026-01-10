using System.Windows;
using LhaHammer.ViewModels;

namespace LhaHammer.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
