using System;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public interface IDictionaryDownloadService
{
    Task EnsureDictionaryExistsAsync(string dictionaryPath, DictionaryDownloadSource downloadSource, Action<string, double>? progressCallback = null);
    bool DictionaryExists(string dictionaryPath);
}
