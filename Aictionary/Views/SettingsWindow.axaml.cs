using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Aictionary.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Aictionary.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private async void BrowseDictionaryButton_Click(object? sender, RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;

        if (!storageProvider.CanPickFolder)
        {
            return;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Dictionary Folder",
            AllowMultiple = false
        });

        if (folders.Count == 1 && DataContext is SettingsViewModel viewModel)
        {
            viewModel.DictionaryPath = folders[0].Path.LocalPath;
        }
    }

    private void OpenDictionaryButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
            return;

        var path = viewModel.DictionaryPath;
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Silently fail if unable to open folder
        }
    }

    private void ResetDictionaryButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.DictionaryPath = Path.Combine(Directory.GetCurrentDirectory(), "dictionary");
        }
    }
}
