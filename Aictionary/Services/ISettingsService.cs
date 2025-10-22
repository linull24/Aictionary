using System;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public interface ISettingsService
{
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    AppSettings CurrentSettings { get; }
    event EventHandler? SettingsChanged;
}
