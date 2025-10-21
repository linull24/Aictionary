using System;
using System.Threading.Tasks;

namespace Aictionary.Services;

public interface IDictionaryDownloadService
{
    Task EnsureDictionaryExistsAsync(Action<string, double>? progressCallback = null);
    bool DictionaryExists();
}
