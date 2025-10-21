using Avalonia.Controls;
using Avalonia.Interactivity;
using Aictionary.ViewModels;
using System.ComponentModel;

namespace Aictionary.Views;

public partial class DownloadProgressWindow : Window
{
    public DownloadProgressWindow()
    {
        InitializeComponent();
        DataContext = new DownloadProgressViewModel();
        Closing += OnClosing;
    }

    public DownloadProgressViewModel ViewModel => (DownloadProgressViewModel)DataContext!;

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // Prevent closing if download is not completed
        if (!ViewModel.IsCompleted)
        {
            e.Cancel = true;
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
