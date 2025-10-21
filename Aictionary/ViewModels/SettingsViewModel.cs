using System;
using System.Reactive;
using System.Reactive.Linq;
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
    private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, IDictionaryService dictionaryService)
    {
        _settingsService = settingsService;
        _dictionaryService = dictionaryService;

        DownloadDictionaryCommand = ReactiveCommand.CreateFromTask(DownloadDictionaryAsync);

        LoadSettings();

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
        set => this.RaiseAndSetIfChanged(ref _model, value);
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

    public ReactiveCommand<Unit, Unit> DownloadDictionaryCommand { get; }

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
}
