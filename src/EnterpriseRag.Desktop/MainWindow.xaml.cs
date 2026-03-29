using System.Windows;
using EnterpriseRag.Desktop.ViewModels;

namespace EnterpriseRag.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
