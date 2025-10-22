using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aictionary.Models;
using Microsoft.Extensions.Configuration;

namespace Aictionary.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly IConfigurationRoot _configuration;
    private AppSettings _currentSettings;

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _settingsFilePath = Path.Combine(appDirectory, "appsettings.json");

        // Ensure settings file exists
        if (!File.Exists(_settingsFilePath))
        {
            var defaultSettings = new AppSettings
            {
                DictionaryPath = Path.Combine(appDirectory, "dictionary"),
                ApiBaseUrl = "https://api.openai.com/v1",
                Model = "gpt-4o-mini"
            };
            var json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }

        _configuration = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _currentSettings = new AppSettings
        {
            DictionaryPath = Path.Combine(appDirectory, "dictionary")
        };

        // Subscribe to configuration changes
        _configuration.GetReloadToken().RegisterChangeCallback(_ =>
        {
            Console.WriteLine("[SettingsService] Configuration file changed, reloading...");
            LoadSettingsFromConfiguration();
        }, null);
    }

    public AppSettings CurrentSettings => _currentSettings;

    public async Task<AppSettings> LoadSettingsAsync()
    {
        await Task.CompletedTask;
        LoadSettingsFromConfiguration();
        return _currentSettings;
    }

    private void LoadSettingsFromConfiguration()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        _currentSettings.ApiBaseUrl = _configuration["ApiBaseUrl"] ?? "https://api.openai.com/v1";
        _currentSettings.ApiKey = _configuration["ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        _currentSettings.Model = _configuration["Model"] ?? "gpt-4o-mini";
        _currentSettings.DictionaryPath = _configuration["DictionaryPath"] ?? Path.Combine(appDirectory, "dictionary");
        _currentSettings.QuickQueryHotkey = _configuration["QuickQueryHotkey"] ?? "Command+Shift+D";
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _currentSettings = settings;

        var settingsToSave = new
        {
            ApiBaseUrl = settings.ApiBaseUrl,
            ApiKey = settings.ApiKey,
            Model = settings.Model,
            DictionaryPath = settings.DictionaryPath,
            QuickQueryHotkey = settings.QuickQueryHotkey
        };

        var json = JsonSerializer.Serialize(settingsToSave, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsFilePath, json);

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
