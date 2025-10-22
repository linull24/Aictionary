using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Data;
using SharpHook.Native;
using SharpHook.Providers;

namespace Aictionary.Services;

public sealed class HotkeyService : IHotkeyService, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, HotkeyRegistration> _hotkeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _activeHotkeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly UioHookProvider _hookProvider;
    private readonly SimpleGlobalHook _hook;
    private Task? _hookTask;
    private bool _disposed;

    public HotkeyService()
    {
        _hookProvider = UioHookProvider.Instance;
        _hookProvider.PromptUserIfAxApiDisabled = false;
        _hook = new SimpleGlobalHook(GlobalHookType.Keyboard, _hookProvider, runAsyncOnBackgroundThread: true);
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.HookDisabled += OnHookDisabled;

        EnsureHookRunning();
    }

    public void RegisterHotkey(string hotkey, Action callback)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            Console.WriteLine("[HotkeyService] Empty hotkey provided");
            return;
        }

        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        var registration = TryCreateRegistration(hotkey, callback);
        if (registration == null)
        {
            Console.WriteLine($"[HotkeyService] Failed to parse hotkey: {hotkey}");
            return;
        }

        lock (_syncRoot)
        {
            _hotkeys[hotkey] = registration;
            _activeHotkeys.Remove(hotkey);
        }

        Console.WriteLine($"[HotkeyService] Registered hotkey: {hotkey}");
        EnsureHookRunning();
    }

    public void UnregisterHotkey(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_hotkeys.Remove(hotkey))
            {
                _activeHotkeys.Remove(hotkey);
                Console.WriteLine($"[HotkeyService] Unregistered hotkey: {hotkey}");
            }
        }
    }

    public void UnregisterAll()
    {
        lock (_syncRoot)
        {
            _hotkeys.Clear();
            _activeHotkeys.Clear();
        }

        Console.WriteLine("[HotkeyService] Cleared all registered hotkeys");
    }

    public bool CheckAccessibilityPermissions()
    {
        var enabled = HasAccessibilityPermission(promptUser: false);

        if (enabled)
        {
            EnsureHookRunning();
            Console.WriteLine("[HotkeyService] Accessibility permissions granted");
        }
        else
        {
            Console.WriteLine("[HotkeyService] Accessibility permissions missing");
        }

        return enabled;
    }

    public void RequestAccessibilityPermissions()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Console.WriteLine("[HotkeyService] Accessibility permissions not required on this platform");
            return;
        }

        Console.WriteLine("[HotkeyService] Opening System Preferences for accessibility permissions...");
        OpenAccessibilityPreferences();

        Console.WriteLine("[HotkeyService] Requesting accessibility permissions via system prompt...");
        var enabled = HasAccessibilityPermission(promptUser: true);

        if (enabled)
        {
            Console.WriteLine("[HotkeyService] Accessibility permissions granted after prompt");
            EnsureHookRunning();
        }
        else
        {
            Console.WriteLine("[HotkeyService] Accessibility permissions still missing after prompt");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _hook.KeyPressed -= OnKeyPressed;
        _hook.KeyReleased -= OnKeyReleased;
        _hook.HookDisabled -= OnHookDisabled;

        try
        {
            if (_hook.IsRunning)
            {
                _hook.Stop();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotkeyService] Error while stopping global hook: {ex.Message}");
        }

        _hook.Dispose();
        lock (_syncRoot)
        {
            _hotkeys.Clear();
            _activeHotkeys.Clear();
        }
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        HotkeyRegistration[] registrations;

        lock (_syncRoot)
        {
            registrations = _hotkeys.Values.ToArray();
        }

        var actualMask = e.RawEvent.Mask;
        foreach (var registration in registrations)
        {
            if (registration.TriggerKey != e.Data.KeyCode)
            {
                continue;
            }

            if (!HasRequiredModifiers(actualMask, registration.RequiredMask))
            {
                continue;
            }

            bool shouldInvoke;
            lock (_syncRoot)
            {
                shouldInvoke = _activeHotkeys.Add(registration.Hotkey);
            }

            if (shouldInvoke)
            {
                Task.Run(() =>
                {
                    try
                    {
                        registration.Callback();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HotkeyService] Hotkey callback error ({registration.Hotkey}): {ex.Message}");
                    }
                });
            }
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        var releasedKey = e.Data.KeyCode;

        lock (_syncRoot)
        {
            foreach (var registration in _hotkeys.Values)
            {
                if (registration.TriggerKey == releasedKey || registration.ModifierKeyCodes.Contains(releasedKey))
                {
                    _activeHotkeys.Remove(registration.Hotkey);
                }
            }
        }
    }

    private void OnHookDisabled(object? sender, HookEventArgs e)
    {
        Console.WriteLine("[HotkeyService] Global hook disabled");
        lock (_syncRoot)
        {
            _hookTask = null;
            _activeHotkeys.Clear();
        }
    }

    private bool HasAccessibilityPermission(bool promptUser)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return true;
        }

        try
        {
            if (promptUser)
            {
                _hookProvider.PromptUserIfAxApiDisabled = true;
            }

            return _hookProvider.IsAxApiEnabled(promptUser);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotkeyService] Accessibility check failed: {ex.Message}");
            return false;
        }
        finally
        {
            if (promptUser)
            {
                _hookProvider.PromptUserIfAxApiDisabled = false;
            }
        }
    }

    private void OpenAccessibilityPreferences()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (process.Start())
            {
                Console.WriteLine("[HotkeyService] System Preferences launched to Accessibility pane");
            }
            else
            {
                Console.WriteLine("[HotkeyService] Failed to start System Preferences process");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotkeyService] Unable to open System Preferences: {ex.Message}");
        }
    }

    private void EnsureHookRunning()
    {
        if (_disposed)
        {
            return;
        }

        if (!HasAccessibilityPermission(promptUser: false))
        {
            Console.WriteLine("[HotkeyService] Global hook not started because accessibility permissions are missing");
            return;
        }

        lock (_syncRoot)
        {
            if (_hookTask is { IsCompleted: false })
            {
                return;
            }

            try
            {
                _hookTask = _hook.RunAsync();
                _hookTask.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        var message = task.Exception?.GetBaseException().Message ?? "Unknown error";
                        Console.WriteLine($"[HotkeyService] Global hook stopped unexpectedly: {message}");
                    }

                    lock (_syncRoot)
                    {
                        _activeHotkeys.Clear();
                        _hookTask = null;
                    }
                }, TaskScheduler.Default);

                Console.WriteLine("[HotkeyService] Global hook started");
            }
            catch (HookException ex)
            {
                Console.WriteLine($"[HotkeyService] Failed to start global hook: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HotkeyService] Unexpected error starting global hook: {ex.Message}");
            }
        }
    }

    private static bool HasRequiredModifiers(EventMask actual, EventMask required)
    {
        if (required == EventMask.None)
        {
            return true;
        }

        if (!CheckModifier(actual, required, EventMask.Shift, EventMask.LeftShift, EventMask.RightShift))
        {
            return false;
        }

        if (!CheckModifier(actual, required, EventMask.Ctrl, EventMask.LeftCtrl, EventMask.RightCtrl))
        {
            return false;
        }

        if (!CheckModifier(actual, required, EventMask.Alt, EventMask.LeftAlt, EventMask.RightAlt))
        {
            return false;
        }

        if (!CheckModifier(actual, required, EventMask.Meta, EventMask.LeftMeta, EventMask.RightMeta))
        {
            return false;
        }

        return true;
    }

    private static bool CheckModifier(EventMask actual, EventMask required, EventMask anyMask, EventMask leftMask, EventMask rightMask) =>
        (required & anyMask) == 0 ||
        (actual & (anyMask | leftMask | rightMask)) != 0;

    private HotkeyRegistration? TryCreateRegistration(string hotkey, Action callback)
    {
        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return null;
        }

        var requiredMask = EventMask.None;
        var modifierKeyCodes = new HashSet<KeyCode>();
        var triggerKey = KeyCode.VcUndefined;

        foreach (var part in parts)
        {
            if (TryApplyModifier(part, ref requiredMask, modifierKeyCodes))
            {
                continue;
            }

            if (TryParseKey(part, out var key))
            {
                triggerKey = key;
                continue;
            }

            Console.WriteLine($"[HotkeyService] Unknown hotkey segment '{part}'");
            return null;
        }

        if (triggerKey == KeyCode.VcUndefined)
        {
            Console.WriteLine("[HotkeyService] Hotkey must include a non-modifier key");
            return null;
        }

        return new HotkeyRegistration
        {
            Hotkey = hotkey,
            TriggerKey = triggerKey,
            RequiredMask = requiredMask,
            ModifierKeyCodes = modifierKeyCodes,
            Callback = callback
        };
    }

    private static bool TryApplyModifier(string token, ref EventMask mask, HashSet<KeyCode> modifierKeyCodes)
    {
        var part = token.Trim();

        if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
        {
            mask |= EventMask.Shift;
            modifierKeyCodes.Add(KeyCode.VcLeftShift);
            modifierKeyCodes.Add(KeyCode.VcRightShift);
            return true;
        }

        if (part.Equals("Control", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
        {
            mask |= EventMask.Ctrl;
            modifierKeyCodes.Add(KeyCode.VcLeftControl);
            modifierKeyCodes.Add(KeyCode.VcRightControl);
            return true;
        }

        if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Option", StringComparison.OrdinalIgnoreCase))
        {
            mask |= EventMask.Alt;
            modifierKeyCodes.Add(KeyCode.VcLeftAlt);
            modifierKeyCodes.Add(KeyCode.VcRightAlt);
            return true;
        }

        if (part.Equals("Command", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Cmd", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Meta", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Super", StringComparison.OrdinalIgnoreCase))
        {
            mask |= EventMask.Meta;
            modifierKeyCodes.Add(KeyCode.VcLeftMeta);
            modifierKeyCodes.Add(KeyCode.VcRightMeta);
            return true;
        }

        return false;
    }

    private static bool TryParseKey(string token, out KeyCode keyCode)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            keyCode = KeyCode.VcUndefined;
            return false;
        }

        var part = token.Trim();

        if (part.Length == 1)
        {
            var character = part[0];

            if (char.IsLetter(character))
            {
                var name = $"Vc{char.ToUpperInvariant(character)}";
                if (Enum.TryParse(name, ignoreCase: true, out keyCode))
                {
                    return true;
                }
            }

            if (char.IsDigit(character))
            {
                var name = $"Vc{character}";
                if (Enum.TryParse(name, ignoreCase: false, out keyCode))
                {
                    return true;
                }
            }
        }

        if (part.Length > 1 && (part[0] == 'F' || part[0] == 'f') && int.TryParse(part.AsSpan(1), out var fnNumber))
        {
            if (fnNumber is >= 1 and <= 24)
            {
                var name = $"VcF{fnNumber}";
                if (Enum.TryParse(name, ignoreCase: false, out keyCode))
                {
                    return true;
                }
            }
        }

        if (SpecialKeys.TryGetValue(part, out keyCode))
        {
            return true;
        }

        var normalized = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(part.ToLowerInvariant()).Replace(" ", string.Empty);
        if (Enum.TryParse($"Vc{normalized}", ignoreCase: false, out keyCode))
        {
            return true;
        }

        keyCode = KeyCode.VcUndefined;
        return false;
    }

    private static readonly Dictionary<string, KeyCode> SpecialKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Space"] = KeyCode.VcSpace,
        ["Spacebar"] = KeyCode.VcSpace,
        ["Enter"] = KeyCode.VcEnter,
        ["Return"] = KeyCode.VcEnter,
        ["Tab"] = KeyCode.VcTab,
        ["Escape"] = KeyCode.VcEscape,
        ["Esc"] = KeyCode.VcEscape,
        ["Backspace"] = KeyCode.VcBackspace,
        ["Delete"] = KeyCode.VcDelete,
        ["Del"] = KeyCode.VcDelete,
        ["Home"] = KeyCode.VcHome,
        ["End"] = KeyCode.VcEnd,
        ["PageUp"] = KeyCode.VcPageUp,
        ["PageDown"] = KeyCode.VcPageDown,
        ["Up"] = KeyCode.VcUp,
        ["Down"] = KeyCode.VcDown,
        ["Left"] = KeyCode.VcLeft,
        ["Right"] = KeyCode.VcRight,
    };

    private sealed class HotkeyRegistration
    {
        public required string Hotkey { get; init; }
        public required KeyCode TriggerKey { get; init; }
        public required EventMask RequiredMask { get; init; }
        public required HashSet<KeyCode> ModifierKeyCodes { get; init; }
        public required Action Callback { get; init; }
    }
}
