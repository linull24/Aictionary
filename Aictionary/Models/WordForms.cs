using System.Text.Json.Serialization;

namespace Aictionary.Models;

public class WordForms
{
    [JsonPropertyName("third_person_singular")]
    public string? ThirdPersonSingular { get; set; }

    [JsonPropertyName("past_tense")]
    public string? PastTense { get; set; }

    [JsonPropertyName("past_participle")]
    public string? PastParticiple { get; set; }

    [JsonPropertyName("present_participle")]
    public string? PresentParticiple { get; set; }
}
