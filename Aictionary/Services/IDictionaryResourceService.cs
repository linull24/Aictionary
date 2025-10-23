using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public interface IDictionaryResourceService
{
    Task<string> GetDictionaryDownloadUrlAsync(DictionaryDownloadSource source = DictionaryDownloadSource.GitHub);
}
