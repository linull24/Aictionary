using System;
using System.Threading.Tasks;
using Aictionary.ViewModels;
using Aictionary.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;

namespace Aictionary.Services;

public class QuickQueryService
{
    private readonly IHotkeyService _hotkeyService;
    private readonly ISettingsService _settingsService;

    public QuickQueryService(IHotkeyService hotkeyService, ISettingsService settingsService)
    {
        _hotkeyService = hotkeyService;
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        RegisterQuickQueryHotkey();

        // Re-register when settings change
        _settingsService.SettingsChanged += (sender, args) =>
        {
            RegisterQuickQueryHotkey();
        };
    }

    private void RegisterQuickQueryHotkey()
    {
        var hotkey = _settingsService.CurrentSettings.QuickQueryHotkey;
        if (string.IsNullOrEmpty(hotkey))
        {
            Console.WriteLine("[QuickQueryService] No hotkey configured");
            return;
        }

        Console.WriteLine($"[QuickQueryService] Registering quick query hotkey: {hotkey}");

        _hotkeyService.UnregisterAll();
        _hotkeyService.RegisterHotkey(hotkey, async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await HandleQuickQuery();
            });
        });
    }

    private async Task HandleQuickQuery()
    {
        try
        {
            Console.WriteLine("[QuickQueryService] Quick query triggered");

            // Get or create main window
            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[QuickQueryService] Could not get main window");
                return;
            }

            // Get the selected text from clipboard via TopLevel
            var topLevel = TopLevel.GetTopLevel(mainWindow);
            var clipboard = topLevel?.Clipboard;
            if (clipboard == null)
            {
                Console.WriteLine("[QuickQueryService] Clipboard not available");
                return;
            }

            // Copy selected text to clipboard (user should have text selected)
            // Note: We can't programmatically copy selected text from other applications
            // User needs to select text first, then press the hotkey
            var text = await clipboard.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("[QuickQueryService] No text in clipboard");
                return;
            }

            Console.WriteLine($"[QuickQueryService] Text from clipboard: '{text}'");

            // Show and activate the window
            if (!mainWindow.IsVisible)
            {
                mainWindow.Show();
            }

            mainWindow.Activate();
            mainWindow.BringIntoView();

            // Set the search text and trigger search
            if (mainWindow.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SearchText = text.Trim();
                // Execute the search command (no await needed, it returns IObservable)
                viewModel.SearchCommand.Execute().Subscribe();
            }

            Console.WriteLine("[QuickQueryService] Quick query completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuickQueryService] Error in HandleQuickQuery: {ex.Message}");
        }
    }

    private MainWindow? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow as MainWindow;
        }

        return null;
    }
}
