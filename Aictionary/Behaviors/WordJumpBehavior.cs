using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Aictionary.ViewModels;
using Avalonia.VisualTree;

namespace Aictionary.Behaviors;

public static class WordJumpBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<SelectableTextBlock, bool>("IsEnabled", typeof(WordJumpBehavior));

    public static bool GetIsEnabled(SelectableTextBlock element)
    {
        return element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(SelectableTextBlock element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    static WordJumpBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<SelectableTextBlock>(OnIsEnabledChanged);
    }

    private static void OnIsEnabledChanged(SelectableTextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            textBlock.PointerMoved += OnPointerMoved;
            textBlock.PointerExited += OnPointerExited;
            // Use AddHandler with handledEventsToo=true to receive events even if SelectableTextBlock handled them
            textBlock.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
        }
        else
        {
            textBlock.PointerMoved -= OnPointerMoved;
            textBlock.PointerExited -= OnPointerExited;
            textBlock.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        }
    }

    private static bool _isModifierPressed = false;

    private static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not SelectableTextBlock textBlock) return;

        var keyModifiers = e.KeyModifiers;
        var isCmdOrCtrlPressed = keyModifiers.HasFlag(KeyModifiers.Meta) || keyModifiers.HasFlag(KeyModifiers.Control);

        if (isCmdOrCtrlPressed)
        {
            _isModifierPressed = true;
            textBlock.Cursor = new Cursor(StandardCursorType.Hand);
            textBlock.TextDecorations = TextDecorations.Underline;
        }
        else
        {
            _isModifierPressed = false;
            textBlock.Cursor = Cursor.Default;
            textBlock.TextDecorations = null;
        }
    }

    private static void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is not SelectableTextBlock textBlock) return;

        textBlock.Cursor = Cursor.Default;
        textBlock.TextDecorations = null;
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not SelectableTextBlock textBlock)
        {
            Console.WriteLine("[WordJumpBehavior] Sender is not SelectableTextBlock");
            return;
        }

        Console.WriteLine("[WordJumpBehavior] PointerPressed event triggered");

        var keyModifiers = e.KeyModifiers;
        var isCmdOrCtrlPressed = keyModifiers.HasFlag(KeyModifiers.Meta) || keyModifiers.HasFlag(KeyModifiers.Control);

        Console.WriteLine($"[WordJumpBehavior] Cmd/Ctrl pressed: {isCmdOrCtrlPressed}, KeyModifiers: {keyModifiers}");

        if (!isCmdOrCtrlPressed)
        {
            Console.WriteLine("[WordJumpBehavior] Cmd/Ctrl not pressed, returning");
            return;
        }

        var point = e.GetCurrentPoint(textBlock);
        if (!point.Properties.IsLeftButtonPressed)
        {
            Console.WriteLine("[WordJumpBehavior] Left button not pressed, returning");
            return;
        }

        // Get the word at the click position
        var word = GetWordAtPosition(textBlock);
        Console.WriteLine($"[WordJumpBehavior] Extracted word: '{word}'");

        if (string.IsNullOrWhiteSpace(word))
        {
            Console.WriteLine("[WordJumpBehavior] Word is null or whitespace, returning");
            return;
        }

        // Only jump if it's an English word (contains only ASCII letters)
        if (!IsEnglishWord(word))
        {
            Console.WriteLine($"[WordJumpBehavior] Word '{word}' is not an English word, returning");
            return;
        }

        Console.WriteLine($"[WordJumpBehavior] Word '{word}' is valid, attempting to find MainWindow");

        // Find the MainWindow and trigger search
        var window = GetParentWindow(textBlock);
        Console.WriteLine($"[WordJumpBehavior] Found window: {window?.GetType().Name ?? "null"}");

        if (window?.DataContext is MainWindowViewModel viewModel)
        {
            Console.WriteLine($"[WordJumpBehavior] Found ViewModel, setting SearchText to '{word}' and executing command");
            viewModel.SearchText = word;
            viewModel.SearchCommand.Execute().Subscribe();
        }
        else
        {
            Console.WriteLine($"[WordJumpBehavior] DataContext is not MainWindowViewModel. DataContext type: {window?.DataContext?.GetType().Name ?? "null"}");
        }

        e.Handled = true;
        Console.WriteLine("[WordJumpBehavior] Event handled");
    }

    private static string? GetWordAtPosition(SelectableTextBlock textBlock)
    {
        // If there's a selection, use the selected text
        var selectedText = textBlock.SelectedText;
        if (!string.IsNullOrWhiteSpace(selectedText))
        {
            // Extract the first English word from selection
            var words = Regex.Matches(selectedText, @"\b[a-zA-Z]+\b");
            if (words.Count > 0)
            {
                return words[0].Value;
            }
        }

        // Otherwise, use the entire text (for comparison words)
        var text = textBlock.Text;
        if (string.IsNullOrWhiteSpace(text)) return null;

        // If the entire text is a single English word, return it
        text = text.Trim();
        if (IsEnglishWord(text))
        {
            return text;
        }

        // Otherwise, extract the first English word from the text
        var wordMatches = Regex.Matches(text, @"\b[a-zA-Z]+\b");
        if (wordMatches.Count > 0)
        {
            return wordMatches[0].Value;
        }

        return null;
    }

    private static bool IsEnglishWord(string word)
    {
        // Check if the word contains only ASCII letters
        return Regex.IsMatch(word, @"^[a-zA-Z]+$");
    }

    private static Window? GetParentWindow(Visual? visual)
    {
        while (visual != null)
        {
            if (visual is Window window)
                return window;
            visual = visual.GetVisualParent();
        }
        return null;
    }
}
