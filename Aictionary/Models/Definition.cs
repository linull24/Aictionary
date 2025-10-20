using System.Text.Json.Serialization;

namespace Aictionary.Models;

public class Definition
{
    [JsonPropertyName("pos")]
    public string Pos { get; set; } = string.Empty;

    [JsonPropertyName("explanation_en")]
    public string ExplanationEn { get; set; } = string.Empty;

    [JsonPropertyName("explanation_cn")]
    public string ExplanationCn { get; set; } = string.Empty;

    [JsonPropertyName("example_en")]
    public string ExampleEn { get; set; } = string.Empty;

    [JsonPropertyName("example_cn")]
    public string ExampleCn { get; set; } = string.Empty;
}
