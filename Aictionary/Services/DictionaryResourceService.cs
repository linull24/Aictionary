using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public class DictionaryResourceService : IDictionaryResourceService
{
    private const string GitHubDictionaryUrl = "https://github.com/ahpxex/open-english-dictionary/releases/download/v1.1/open-english-dictionary.zip";
    private const string GiteeDictionaryUrl = "https://gitee.com/fanxiao25/open-english-dictionary/releases/download/v1.1/open-english-dictionary.zip";

    public Task<string> GetDictionaryDownloadUrlAsync(DictionaryDownloadSource source = DictionaryDownloadSource.GitHub)
    {
        var url = source switch
        {
            DictionaryDownloadSource.Gitee => GiteeDictionaryUrl,
            _ => GitHubDictionaryUrl
        };

        return Task.FromResult(url);
    }
}
