using System;
using System.Threading.Tasks;

namespace Aictionary.Services;

public interface IDictionaryDownloadService
{
    Task EnsureDictionaryExistsAsync(string dictionaryPath, Action<string, double>? progressCallback = null);
    bool DictionaryExists(string dictionaryPath);
}
