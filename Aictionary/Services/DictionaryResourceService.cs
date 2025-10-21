using System.Threading.Tasks;

namespace Aictionary.Services;

public class DictionaryResourceService : IDictionaryResourceService
{
    private const string DefaultDictionaryUrl = "https://github.com/ahpxex/open-e2c-dictionary/releases/download/1.0/dictionary.zip";

    public Task<string> GetDictionaryDownloadUrlAsync()
    {
        return Task.FromResult(DefaultDictionaryUrl);
    }
}
