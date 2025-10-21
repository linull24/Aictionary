using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Aictionary.Models;
using Aictionary.Services;
using ReactiveUI;

namespace Aictionary.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IDictionaryService _dictionaryService;

    private string _apiBaseUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _model = string.Empty;
    private string _dictionaryPath = string.Empty;
    private ObservableCollection<string> _cachedWords = new();
    private string? _selectedWord;
    private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, IDictionaryService dictionaryService)
    {
        _settingsService = settingsService;
        _dictionaryService = dictionaryService;

        SaveCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
        LoadCachedWordsCommand = ReactiveCommand.CreateFromTask(LoadCachedWordsAsync);
        DeleteSelectedWordCommand = ReactiveCommand.CreateFromTask(
            DeleteSelectedWordAsync,
            this.WhenAnyValue(x => x.SelectedWord, word => !string.IsNullOrWhiteSpace(word))
        );
        BrowseDictionaryPathCommand = ReactiveCommand.Create(BrowseDictionaryPath);
        DownloadDictionaryCommand = ReactiveCommand.CreateFromTask(DownloadDictionaryAsync);

        LoadSettings();
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => this.RaiseAndSetIfChanged(ref _apiBaseUrl, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => this.RaiseAndSetIfChanged(ref _apiKey, value);
    }

    public string Model
    {
        get => _model;
        set => this.RaiseAndSetIfChanged(ref _model, value);
    }

    public string DictionaryPath
    {
        get => _dictionaryPath;
        set => this.RaiseAndSetIfChanged(ref _dictionaryPath, value);
    }

    public ObservableCollection<string> CachedWords
    {
        get => _cachedWords;
        set => this.RaiseAndSetIfChanged(ref _cachedWords, value);
    }

    public string? SelectedWord
    {
        get => _selectedWord;
        set => this.RaiseAndSetIfChanged(ref _selectedWord, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadCachedWordsCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedWordCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseDictionaryPathCommand { get; }
    public ReactiveCommand<Unit, Unit> DownloadDictionaryCommand { get; }

    private void LoadSettings()
    {
        var settings = _settingsService.CurrentSettings;
        ApiBaseUrl = settings.ApiBaseUrl;
        ApiKey = settings.ApiKey;
        Model = settings.Model;
        DictionaryPath = settings.DictionaryPath;
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new AppSettings
            {
                ApiBaseUrl = ApiBaseUrl,
                ApiKey = ApiKey,
                Model = Model,
                DictionaryPath = DictionaryPath
            };

            await _settingsService.SaveSettingsAsync(settings);
            StatusMessage = "Settings saved successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
    }

    private async Task LoadCachedWordsAsync()
    {
        try
        {
            var words = await _dictionaryService.GetCachedWordsAsync();
            CachedWords.Clear();
            foreach (var word in words)
            {
                CachedWords.Add(word);
            }
            StatusMessage = $"Loaded {words.Count} cached words.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading cached words: {ex.Message}";
        }
    }

    private async Task DeleteSelectedWordAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedWord))
            return;

        try
        {
            var success = await _dictionaryService.DeleteCachedWordAsync(SelectedWord);
            if (success)
            {
                CachedWords.Remove(SelectedWord);
                StatusMessage = $"Deleted '{SelectedWord}' from cache.";
                SelectedWord = null;
            }
            else
            {
                StatusMessage = $"Failed to delete '{SelectedWord}'.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting word: {ex.Message}";
        }
    }

    private void BrowseDictionaryPath()
    {
        // This would typically open a folder browser dialog
        // For now, it's a placeholder
        StatusMessage = "Folder browser not yet implemented. Please edit the path manually.";
    }

    private async Task DownloadDictionaryAsync()
    {
        System.Console.WriteLine("[SettingsViewModel] DownloadDictionaryAsync started");
        try
        {
            System.Console.WriteLine("[SettingsViewModel] Creating services");
            var resourceService = new DictionaryResourceService();
            var downloadService = new DictionaryDownloadService(resourceService);

            System.Console.WriteLine("[SettingsViewModel] Creating download window");
            var downloadWindow = new Views.DownloadProgressWindow();
            
            System.Console.WriteLine("[SettingsViewModel] Showing download window");
            downloadWindow.Show();

            System.Console.WriteLine("[SettingsViewModel] Starting download");
            await downloadService.EnsureDictionaryExistsAsync((message, progress) =>
            {
                System.Console.WriteLine($"[SettingsViewModel] Progress callback: {message} ({progress}%)");
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    System.Console.WriteLine($"[SettingsViewModel] UI Thread updating: {message}");
                    downloadWindow.ViewModel.StatusMessage = message;
                    downloadWindow.ViewModel.Progress = progress;
                    downloadWindow.ViewModel.IsIndeterminate = progress < 10;

                    if (progress >= 100)
                    {
                        System.Console.WriteLine("[SettingsViewModel] Download completed!");
                        downloadWindow.ViewModel.IsCompleted = true;
                        StatusMessage = "Dictionary downloaded successfully!";
                    }
                });
            });
            System.Console.WriteLine("[SettingsViewModel] Download finished successfully");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[SettingsViewModel] Download ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Console.WriteLine($"[SettingsViewModel] Stack trace: {ex.StackTrace}");
            StatusMessage = $"Error downloading dictionary: {ex.Message}";
        }
    }
}
