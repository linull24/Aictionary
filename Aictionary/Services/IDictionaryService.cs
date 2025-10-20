using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public interface IDictionaryService
{
    Task<WordDefinition?> GetDefinitionAsync(string word);
}
