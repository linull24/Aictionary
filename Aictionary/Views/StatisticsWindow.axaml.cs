using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Aictionary.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Aictionary.Views;

public partial class StatisticsWindow : Window
{
    private bool _isInitialized;

    public StatisticsWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_isInitialized)
        {
            return;
        }

        if (DataContext is StatisticsViewModel viewModel)
        {
            try
            {
                await viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[StatisticsWindow] LoadAsync ERROR: {ex.Message}");
            }
        }

        _isInitialized = true;
    }

    private async void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StatisticsViewModel viewModel)
            return;

        var storageProvider = StorageProvider;

        if (!storageProvider.CanSave)
            return;

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Words to TXT",
            SuggestedFileName = "words.txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text File")
                {
                    Patterns = new[] { "*.txt" }
                }
            }
        });

        if (file == null)
            return;

        try
        {
            var words = viewModel.GetAllWords();
            var content = string.Join(Environment.NewLine, words);
            var filePath = file.Path.LocalPath;
            await File.WriteAllTextAsync(filePath, content);

            // Open the file location in Finder/Explorer
            OpenFileLocation(filePath);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[StatisticsWindow] Export ERROR: {ex.Message}");
        }
    }

    private void OpenFileLocation(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-R \"{filePath}\"",
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = directory,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[StatisticsWindow] Open location ERROR: {ex.Message}");
        }
    }

    private async void DeleteWordButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string word)
            return;

        if (DataContext is not StatisticsViewModel viewModel)
            return;

        try
        {
            await viewModel.RemoveWordAsync(word);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[StatisticsWindow] Delete word ERROR: {ex.Message}");
        }
    }
}
