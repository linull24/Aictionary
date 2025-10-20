using System.Text.Json.Serialization;

namespace Aictionary.Models;

public class WordComparison
{
    [JsonPropertyName("word_to_compare")]
    public string WordToCompare { get; set; } = string.Empty;

    [JsonPropertyName("analysis")]
    public string Analysis { get; set; } = string.Empty;
}
