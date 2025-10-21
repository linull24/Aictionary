using Avalonia.Controls;
using Avalonia.Interactivity;
using Aictionary.ViewModels;

namespace Aictionary.Views;

public partial class DownloadProgressWindow : Window
{
    public DownloadProgressWindow()
    {
        InitializeComponent();
        DataContext = new DownloadProgressViewModel();
    }

    public DownloadProgressViewModel ViewModel => (DownloadProgressViewModel)DataContext!;

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
