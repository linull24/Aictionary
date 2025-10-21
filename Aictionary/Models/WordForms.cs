using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Aictionary.Models;

public class WordForms : Dictionary<string, object>
{
    // Helper method to get value as string (for single values)
    public string? GetString(string key)
    {
        if (!TryGetValue(key, out var value))
            return null;

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString();
            if (element.ValueKind == JsonValueKind.Array)
                return string.Join(", ", element.EnumerateArray().Select(e => e.GetString() ?? ""));
        }

        return value?.ToString();
    }

    // Helper method to get value as string array (for array values like variant)
    public List<string>? GetStringList(string key)
    {
        if (!TryGetValue(key, out var value))
            return null;

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
                return element.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
            if (element.ValueKind == JsonValueKind.String)
                return new List<string> { element.GetString() ?? "" };
        }

        if (value is string str)
            return new List<string> { str };

        return null;
    }
}
