using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public interface IQueryHistoryService
{
    Task AddEntryAsync(string word, DateTime queriedAt, string? conciseDefinition = null);
    Task<IReadOnlyList<QueryHistoryEntry>> GetEntriesAsync();
    Task RemoveWordEntriesAsync(string word);
}
