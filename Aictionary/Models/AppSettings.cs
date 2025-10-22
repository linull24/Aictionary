using System.Text.Json.Serialization;

namespace Aictionary.Models;

public class AppSettings
{
    [JsonPropertyName("api_base_url")]
    public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1";

    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o-mini";

    [JsonPropertyName("dictionary_path")]
    public string DictionaryPath { get; set; } = string.Empty;

    [JsonPropertyName("quick_query_hotkey")]
    public string QuickQueryHotkey { get; set; } = "Command+Shift+D";
}
