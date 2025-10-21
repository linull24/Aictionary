using System.Collections.Generic;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public interface IOpenAIService
{
    Task<WordDefinition?> GenerateDefinitionAsync(string word);
    Task<List<string>> GetAvailableModelsAsync();
}
