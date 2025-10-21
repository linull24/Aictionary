using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _currentSettings;

    public SettingsService()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _settingsFilePath = Path.Combine(appDirectory, "settings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        _currentSettings = new AppSettings
        {
            DictionaryPath = Path.Combine(appDirectory, "dictionary")
        };
    }

    public AppSettings CurrentSettings => _currentSettings;

    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (settings != null)
                {
                    _currentSettings = settings;

                    // Ensure dictionary path is set
                    if (string.IsNullOrEmpty(_currentSettings.DictionaryPath))
                    {
                        _currentSettings.DictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dictionary");
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, use default settings
            }
        }

        // Get API key from environment if not set in settings
        if (string.IsNullOrEmpty(_currentSettings.ApiKey))
        {
            _currentSettings.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        }

        return _currentSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _currentSettings = settings;
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_settingsFilePath, json);
    }
}
