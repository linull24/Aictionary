using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aictionary.Services;
using Aictionary.ViewModels;
using Aictionary.Views;

namespace Aictionary;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Initialize services
            var dictionaryService = new DictionaryService();

            // Get OpenAI API key from environment variable
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
            var openAIService = new OpenAIService(apiKey);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(dictionaryService, openAIService),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
