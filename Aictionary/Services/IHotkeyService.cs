using System;

namespace Aictionary.Services;

public interface IHotkeyService
{
    void RegisterHotkey(string hotkey, Action callback);
    void UnregisterHotkey(string hotkey);
    void UnregisterAll();
    bool CheckAccessibilityPermissions();
    void RequestAccessibilityPermissions();
}
