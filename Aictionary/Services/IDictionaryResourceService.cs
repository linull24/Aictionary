using System.Threading.Tasks;

namespace Aictionary.Services;

public interface IDictionaryResourceService
{
    Task<string> GetDictionaryDownloadUrlAsync();
}
