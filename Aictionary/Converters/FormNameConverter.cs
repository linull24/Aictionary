using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Aictionary.Converters;

public class FormNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string formName)
            return value;

        // Convert snake_case to Title Case
        // e.g., "third_person_singular" -> "Third Person Singular"
        var words = formName.Split('_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
        }
        return string.Join(" ", words);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack is not needed for one-way display bindings
        return value;
    }
}
