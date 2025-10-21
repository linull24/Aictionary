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
        if (sender is not SelectableTextBlock textBlock) return;

        var keyModifiers = e.KeyModifiers;
        var isCmdOrCtrlPressed = keyModifiers.HasFlag(KeyModifiers.Meta) || keyModifiers.HasFlag(KeyModifiers.Control);

        if (!isCmdOrCtrlPressed) return;

        var point = e.GetCurrentPoint(textBlock);
        if (!point.Properties.IsLeftButtonPressed) return;

        // Get the word at the click position
        var word = GetWordAtPosition(textBlock);
        if (string.IsNullOrWhiteSpace(word)) return;

        // Only jump if it's an English word (contains only ASCII letters)
        if (!IsEnglishWord(word)) return;

        // Find the MainWindow and trigger search
        var window = GetParentWindow(textBlock);
        if (window?.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SearchText = word;
            viewModel.SearchCommand.Execute().Subscribe();
        }

        e.Handled = true;
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
