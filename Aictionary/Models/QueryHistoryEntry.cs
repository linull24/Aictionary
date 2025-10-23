using System;
using System.Text.Json.Serialization;

namespace Aictionary.Models;

public class QueryHistoryEntry
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("queried_at")]
    public DateTime QueriedAt { get; set; }

    [JsonPropertyName("concise_definition")]
    public string? ConciseDefinition { get; set; }
}
