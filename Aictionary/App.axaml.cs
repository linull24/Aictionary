using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aictionary.Services;
using Aictionary.ViewModels;
using Aictionary.Views;

namespace Aictionary;

public partial class App : Application
{
    private static ISettingsService? _settingsService;
    private static IDictionaryService? _dictionaryService;
    private static IOpenAIService? _openAIService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        System.Console.WriteLine("[App] OnFrameworkInitializationCompleted started");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            System.Console.WriteLine("[App] Desktop lifetime detected");

            // Ensure dictionary exists before initializing services
            var resourceService = new DictionaryResourceService();
            System.Console.WriteLine("[App] DictionaryResourceService created");

            var downloadService = new DictionaryDownloadService(resourceService);
            System.Console.WriteLine("[App] DictionaryDownloadService created");
            
            var dictionaryExists = downloadService.DictionaryExists();
            System.Console.WriteLine($"[App] Dictionary exists: {dictionaryExists}");

            if (!dictionaryExists)
            {
                System.Console.WriteLine("[App] Creating download window");
                var downloadWindow = new Views.DownloadProgressWindow();
                
                System.Console.WriteLine("[App] Showing download window");
                downloadWindow.Show();
                
                try
                {
                    System.Console.WriteLine("[App] Starting download...");
                    await downloadService.EnsureDictionaryExistsAsync((message, progress) =>
                    {
                        System.Console.WriteLine($"[App] Progress callback: {message} ({progress}%)");
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            System.Console.WriteLine($"[App] UI Thread updating: {message}");
                            downloadWindow.ViewModel.StatusMessage = message;
                            downloadWindow.ViewModel.Progress = progress;
                            downloadWindow.ViewModel.IsIndeterminate = progress < 10;
                            
                            if (progress >= 100)
                            {
                                System.Console.WriteLine("[App] Download completed!");
                                downloadWindow.ViewModel.IsCompleted = true;
                            }
                        });
                    });
                    System.Console.WriteLine("[App] Download finished successfully");
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"[App] Download ERROR: {ex.GetType().Name}: {ex.Message}");
                    System.Console.WriteLine($"[App] Stack trace: {ex.StackTrace}");
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        downloadWindow.ViewModel.StatusMessage = $"Error: {ex.Message}";
                        downloadWindow.ViewModel.IsCompleted = true;
                        downloadWindow.ViewModel.IsIndeterminate = false;
                    });
                }
            }

            System.Console.WriteLine("[App] Initializing services...");
            // Initialize services
            _settingsService = new SettingsService();
            await _settingsService.LoadSettingsAsync();

            _dictionaryService = new DictionaryService(_settingsService);
            _openAIService = new OpenAIService(_settingsService);

            System.Console.WriteLine("[App] Creating main window");
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_dictionaryService, _openAIService, _settingsService),
            };
            System.Console.WriteLine("[App] Main window created");
        }

        base.OnFrameworkInitializationCompleted();
        System.Console.WriteLine("[App] OnFrameworkInitializationCompleted finished");
    }

    public static SettingsWindow CreateSettingsWindow()
    {
        if (_settingsService == null || _dictionaryService == null || _openAIService == null)
        {
            throw new System.InvalidOperationException("Services not initialized");
        }

        return new SettingsWindow
        {
            DataContext = new SettingsViewModel(_settingsService, _dictionaryService, _openAIService)
        };
    }
}
