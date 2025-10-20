using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aictionary.Models;

public class WordDefinition
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("pronunciation")]
    public string Pronunciation { get; set; } = string.Empty;

    [JsonPropertyName("concise_definition")]
    public string ConciseDefinition { get; set; } = string.Empty;

    [JsonPropertyName("forms")]
    public WordForms? Forms { get; set; }

    [JsonPropertyName("definitions")]
    public List<Definition> Definitions { get; set; } = new();

    [JsonPropertyName("comparison")]
    public List<WordComparison> Comparison { get; set; } = new();
}
