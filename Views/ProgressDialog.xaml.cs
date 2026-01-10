using System.Windows;
using LhaHammer.ViewModels;

namespace LhaHammer.Views;

public partial class ProgressDialog : Window
{
    public ProgressDialog(ProgressViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
