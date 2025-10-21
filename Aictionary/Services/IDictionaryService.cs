using System.Collections.Generic;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public interface IDictionaryService
{
    Task<WordDefinition?> GetDefinitionAsync(string word);
    Task<List<string>> GetCachedWordsAsync();
    Task<bool> DeleteCachedWordAsync(string word);
    Task<bool> SaveDefinitionAsync(WordDefinition definition);
}
