using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Aictionary.ViewModels;
using Aictionary.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using SharpHook;
using SharpHook.Data;
using SharpHook.Native;

namespace Aictionary.Services;

public class QuickQueryService
{
    private readonly IHotkeyService _hotkeyService;
    private readonly ISettingsService _settingsService;
    private readonly EventSimulator _eventSimulator;

    public QuickQueryService(IHotkeyService hotkeyService, ISettingsService settingsService)
    {
        _hotkeyService = hotkeyService;
        _settingsService = settingsService;
        _eventSimulator = new EventSimulator();
    }

    public void Initialize()
    {
        // Check permissions before registering
        if (!_hotkeyService.CheckAccessibilityPermissions())
        {
            Console.WriteLine("[QuickQueryService] Accessibility permissions not granted. Hotkey not registered.");
            Console.WriteLine("[QuickQueryService] Please grant accessibility permissions in Settings > Keyboard.");
            return;
        }

        RegisterQuickQueryHotkey();

        // Re-register when settings change
        _settingsService.SettingsChanged += (sender, args) =>
        {
            RegisterQuickQueryHotkey();
        };
    }

    public void ReregisterHotkey()
    {
        // This method can be called after permissions are granted
        if (_hotkeyService.CheckAccessibilityPermissions())
        {
            RegisterQuickQueryHotkey();
        }
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

            // Step 1: Simulate Cmd+C (or Ctrl+C on Windows/Linux) to copy selected text
            await SimulateCopyShortcut();

            // Wait a bit for the copy operation to complete
            await Task.Delay(100);

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

            // Get text from clipboard
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

    private async Task SimulateCopyShortcut()
    {
        try
        {
            Console.WriteLine("[QuickQueryService] Simulating copy shortcut...");

            // Determine which modifier key to use based on OS
            var modifierKeyCode = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? KeyCode.VcLeftMeta  // Command on macOS
                : KeyCode.VcLeftControl;  // Ctrl on Windows/Linux

            // Press modifier key (Command or Ctrl)
            _eventSimulator.SimulateKeyPress(modifierKeyCode);
            await Task.Delay(50);

            // Press C key
            _eventSimulator.SimulateKeyPress(KeyCode.VcC);
            await Task.Delay(50);

            // Release C key
            _eventSimulator.SimulateKeyRelease(KeyCode.VcC);
            await Task.Delay(50);

            // Release modifier key
            _eventSimulator.SimulateKeyRelease(modifierKeyCode);

            Console.WriteLine("[QuickQueryService] Copy shortcut simulated");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuickQueryService] Error simulating copy shortcut: {ex.Message}");
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
