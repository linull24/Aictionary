using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Aictionary.Models;
using Aictionary.Services;
using DynamicData;
using ReactiveUI;

namespace Aictionary.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IDictionaryService _dictionaryService;
    private readonly IOpenAIService _openAIService;

    private string _apiBaseUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _model = string.Empty;
    private string _dictionaryPath = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isApiKeyVisible = false;
    private bool _isLoadingModels = false;
    private string _searchText = string.Empty;
    private bool _isLoadingCachedWords = false;

    private readonly ObservableCollection<string> _availableModels = new();
    private readonly ObservableCollection<string> _cachedWords = new();
    private readonly ObservableCollection<string> _filteredCachedWords = new();

    public SettingsViewModel(ISettingsService settingsService, IDictionaryService dictionaryService, IOpenAIService openAIService)
    {
        _settingsService = settingsService;
        _dictionaryService = dictionaryService;
        _openAIService = openAIService;

        DownloadDictionaryCommand = ReactiveCommand.CreateFromTask(DownloadDictionaryAsync);
        RefreshModelsCommand = ReactiveCommand.CreateFromTask(RefreshModelsAsync);
        ToggleApiKeyVisibilityCommand = ReactiveCommand.Create(ToggleApiKeyVisibility);
        RefreshCachedWordsCommand = ReactiveCommand.CreateFromTask(RefreshCachedWordsAsync);

        LoadSettings();

        // Auto-load cached words on startup
        _ = Task.Run(async () =>
        {
            await RefreshCachedWordsAsync();
        });

        // Initialize available models with current model if it exists
        if (!string.IsNullOrEmpty(Model))
        {
            _availableModels.Add(Model);
        }

        // Auto-load models list only if no model is selected
        if (string.IsNullOrEmpty(Model))
        {
            _ = Task.Run(async () =>
            {
                await RefreshModelsAsync();
            });
        }

        // Auto-save settings when any property changes
        this.WhenAnyValue(
            x => x.ApiBaseUrl,
            x => x.ApiKey,
            x => x.Model,
            x => x.DictionaryPath
        )
        .Skip(1) // Skip the initial load
        .Throttle(TimeSpan.FromMilliseconds(500)) // Debounce to avoid saving too frequently
        .SelectMany(_ => System.Reactive.Linq.Observable.FromAsync(() => AutoSaveSettingsAsync()))
        .Subscribe(
            _ => { },
            ex => System.Console.WriteLine($"[SettingsViewModel] Auto-save subscription ERROR: {ex.Message}")
        );
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
        set
        {
            System.Console.WriteLine($"[Model Property] Setting Model from '{_model}' to '{value}'");
            this.RaiseAndSetIfChanged(ref _model, value);
        }
    }

    public string DictionaryPath
    {
        get => _dictionaryPath;
        set => this.RaiseAndSetIfChanged(ref _dictionaryPath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsApiKeyVisible
    {
        get => _isApiKeyVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isApiKeyVisible, value);
            this.RaisePropertyChanged(nameof(ApiKeyToggleButtonText));
        }
    }

    public string ApiKeyToggleButtonText => IsApiKeyVisible ? "Hide" : "Show";

    public bool IsLoadingModels
    {
        get => _isLoadingModels;
        set => this.RaiseAndSetIfChanged(ref _isLoadingModels, value);
    }

    public ObservableCollection<string> AvailableModels => _availableModels;

    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterCachedWords();
        }
    }

    public bool IsLoadingCachedWords
    {
        get => _isLoadingCachedWords;
        set => this.RaiseAndSetIfChanged(ref _isLoadingCachedWords, value);
    }

    public ObservableCollection<string> CachedWords => _cachedWords;
    public ObservableCollection<string> FilteredCachedWords => _filteredCachedWords;

    public ReactiveCommand<Unit, Unit> DownloadDictionaryCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshModelsCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleApiKeyVisibilityCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCachedWordsCommand { get; }

    private void LoadSettings()
    {
        var settings = _settingsService.CurrentSettings;
        ApiBaseUrl = settings.ApiBaseUrl;
        ApiKey = settings.ApiKey;
        Model = settings.Model;
        DictionaryPath = settings.DictionaryPath;
    }

    private async Task AutoSaveSettingsAsync()
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
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[SettingsViewModel] Auto-save ERROR: {ex.Message}");
        }
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

    private async Task RefreshModelsAsync()
    {
        try
        {
            IsLoadingModels = true;
            StatusMessage = "Fetching available models...";

            System.Console.WriteLine($"[RefreshModels] START - Current Model: '{Model}'");

            // Save the current model selection before clearing
            var selectedModel = Model;
            System.Console.WriteLine($"[RefreshModels] Saved selected model: '{selectedModel}'");

            var models = await _openAIService.GetAvailableModelsAsync();

            System.Console.WriteLine($"[RefreshModels] Fetched {models.Count} models from API");
            System.Console.WriteLine($"[RefreshModels] Before Clear - AvailableModels count: {_availableModels.Count}");

            _availableModels.Clear();

            System.Console.WriteLine($"[RefreshModels] After Clear - Current Model is now: '{Model}'");

            // Add the saved model first if it's not in the list
            if (!string.IsNullOrEmpty(selectedModel) && !models.Contains(selectedModel))
            {
                System.Console.WriteLine($"[RefreshModels] Adding saved model '{selectedModel}' to list (not in API response)");
                _availableModels.Add(selectedModel);
            }
            else if (!string.IsNullOrEmpty(selectedModel))
            {
                System.Console.WriteLine($"[RefreshModels] Saved model '{selectedModel}' is already in API response");
            }

            _availableModels.AddRange(models);

            System.Console.WriteLine($"[RefreshModels] After AddRange - AvailableModels count: {_availableModels.Count}");

            // Restore the selected model
            if (!string.IsNullOrEmpty(selectedModel))
            {
                System.Console.WriteLine($"[RefreshModels] Restoring Model to: '{selectedModel}'");
                Model = selectedModel;
            }

            System.Console.WriteLine($"[RefreshModels] END - Current Model: '{Model}'");

            StatusMessage = models.Count > 0
                ? $"Loaded {models.Count} models successfully"
                : "No models found. Please check your API key and base URL.";
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[RefreshModels] ERROR: {ex.Message}");
            StatusMessage = $"Error fetching models: {ex.Message}";

            // If fetching fails and we have a current model, ensure it's in the list
            if (!string.IsNullOrEmpty(Model) && !_availableModels.Contains(Model))
            {
                System.Console.WriteLine($"[RefreshModels] Exception handler - Adding model '{Model}' to list");
                _availableModels.Add(Model);
            }
        }
        finally
        {
            IsLoadingModels = false;
            System.Console.WriteLine($"[RefreshModels] FINALLY - IsLoadingModels set to false");
        }
    }

    private void ToggleApiKeyVisibility()
    {
        IsApiKeyVisible = !IsApiKeyVisible;
    }

    private async Task RefreshCachedWordsAsync()
    {
        try
        {
            IsLoadingCachedWords = true;

            var words = await _dictionaryService.GetCachedWordsAsync();

            _cachedWords.Clear();
            _cachedWords.AddRange(words);

            FilterCachedWords();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[SettingsViewModel] Error loading cached words: {ex.Message}");
        }
        finally
        {
            IsLoadingCachedWords = false;
        }
    }

    private void FilterCachedWords()
    {
        _filteredCachedWords.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _filteredCachedWords.AddRange(_cachedWords);
        }
        else
        {
            var filtered = _cachedWords.Where(word =>
                word.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            _filteredCachedWords.AddRange(filtered);
        }
    }
}
