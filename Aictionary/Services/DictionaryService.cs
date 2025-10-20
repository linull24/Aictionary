using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public class DictionaryService : IDictionaryService
{
    private readonly string _dictionaryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public DictionaryService()
    {
        _dictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dictionary");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<WordDefinition?> GetDefinitionAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return null;

        var fileName = $"{word.ToLower()}.json";
        var filePath = Path.Combine(_dictionaryPath, fileName);

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
}
