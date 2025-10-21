using System;
using System.ClientModel;
using System.Text.Json;
using System.Threading.Tasks;
using Aictionary.Models;
using OpenAI;
using OpenAI.Chat;

namespace Aictionary.Services;

public class OpenAIService : IOpenAIService
{
    private readonly ISettingsService _settingsService;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenAIService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private ChatClient CreateChatClient()
    {
        var settings = _settingsService.CurrentSettings;
        var apiKey = settings.ApiKey;
        var model = settings.Model;

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please set it in Settings.");
        }

        // Create OpenAI client with custom endpoint if specified
        if (!string.IsNullOrEmpty(settings.ApiBaseUrl) &&
            settings.ApiBaseUrl != "https://api.openai.com/v1")
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(settings.ApiBaseUrl)
            };
            var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
            return client.GetChatClient(model);
        }

        return new ChatClient(model, apiKey);
    }

    public async Task<WordDefinition?> GenerateDefinitionAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return null;

        try
        {
            var chatClient = CreateChatClient();

            var prompt = $@"Please provide a comprehensive definition for the word ""{word}"" in the following JSON format:
{{
  ""word"": ""{word}"",
  ""pronunciation"": ""phonetic pronunciation"",
  ""concise_definition"": ""brief definition with part of speech and Chinese translation"",
  ""forms"": {{
    ""third_person_singular"": ""form if applicable"",
    ""past_tense"": ""form if applicable"",
    ""past_participle"": ""form if applicable"",
    ""present_participle"": ""form if applicable""
  }},
  ""definitions"": [
    {{
      ""pos"": ""part of speech"",
      ""explanation_en"": ""English explanation"",
      ""explanation_cn"": ""Chinese explanation"",
      ""example_en"": ""English example sentence"",
      ""example_cn"": ""Chinese translation of example""
    }}
  ],
  ""comparison"": [
    {{
      ""word_to_compare"": ""similar word"",
      ""analysis"": ""comparison analysis in Chinese""
    }}
  ]
}}

Please provide at least 2-3 different definitions if the word has multiple meanings, and 2-3 similar words for comparison. Ensure all fields are filled appropriately. Return ONLY the JSON, no additional text.";

            var completion = await chatClient.CompleteChatAsync(prompt);
            var response = completion.Value.Content[0].Text;

            // Try to extract JSON if there's markdown code block
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd >= 0)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<WordDefinition>(jsonContent, _jsonOptions);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
