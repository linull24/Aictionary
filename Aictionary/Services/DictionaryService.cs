using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public class DictionaryService : IDictionaryService
{
    private readonly ISettingsService _settingsService;
    private readonly JsonSerializerOptions _jsonOptions;

    public DictionaryService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<WordDefinition?> GetDefinitionAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return null;

        var dictionaryPath = _settingsService.CurrentSettings.DictionaryPath;
        if (string.IsNullOrEmpty(dictionaryPath) || !Directory.Exists(dictionaryPath))
            return null;

        var fileName = $"{word.ToLower()}.json";
        var filePath = Path.Combine(dictionaryPath, fileName);

        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<WordDefinition>(json, _jsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<string>> GetCachedWordsAsync()
    {
        var dictionaryPath = _settingsService.CurrentSettings.DictionaryPath;
        if (string.IsNullOrEmpty(dictionaryPath) || !Directory.Exists(dictionaryPath))
            return new List<string>();

        try
        {
            var files = Directory.GetFiles(dictionaryPath, "*.json");
            return files.Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(w => w).ToList();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    public async Task<bool> DeleteCachedWordAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        var dictionaryPath = _settingsService.CurrentSettings.DictionaryPath;
        if (string.IsNullOrEmpty(dictionaryPath) || !Directory.Exists(dictionaryPath))
            return false;

        var fileName = $"{word.ToLower()}.json";
        var filePath = Path.Combine(dictionaryPath, fileName);

        if (!File.Exists(filePath))
            return false;

        try
        {
            File.Delete(filePath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> SaveDefinitionAsync(WordDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.Word))
            return false;

        var dictionaryPath = _settingsService.CurrentSettings.DictionaryPath;
        if (string.IsNullOrEmpty(dictionaryPath))
            return false;

        try
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(dictionaryPath))
            {
                Directory.CreateDirectory(dictionaryPath);
            }

            var fileName = $"{definition.Word.ToLower()}.json";
            var filePath = Path.Combine(dictionaryPath, fileName);

            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
