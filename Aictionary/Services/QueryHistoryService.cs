using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public class QueryHistoryService : IQueryHistoryService
{
    private readonly string _historyFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);

    public QueryHistoryService()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _historyFilePath = Path.Combine(appDirectory, "query-history.json");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        if (!File.Exists(_historyFilePath))
        {
            var emptyJson = JsonSerializer.Serialize(new List<QueryHistoryEntry>(), _jsonOptions);
            File.WriteAllText(_historyFilePath, emptyJson);
        }
    }

    public async Task AddEntryAsync(string word, DateTime queriedAt)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        var entry = new QueryHistoryEntry
        {
            Word = word.Trim(),
            QueriedAt = queriedAt
        };

        await _fileSemaphore.WaitAsync();

        try
        {
            var entries = await ReadEntriesInternalAsync();
            entries.Add(entry);
            await WriteEntriesInternalAsync(entries);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    public async Task<IReadOnlyList<QueryHistoryEntry>> GetEntriesAsync()
    {
        await _fileSemaphore.WaitAsync();

        try
        {
            var entries = await ReadEntriesInternalAsync();
            return entries.AsReadOnly();
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private async Task<List<QueryHistoryEntry>> ReadEntriesInternalAsync()
    {
        try
        {
            var json = await File.ReadAllTextAsync(_historyFilePath);
            var entries = JsonSerializer.Deserialize<List<QueryHistoryEntry>>(json, _jsonOptions);
            return entries ?? new List<QueryHistoryEntry>();
        }
        catch (Exception)
        {
            return new List<QueryHistoryEntry>();
        }
    }

    private async Task WriteEntriesInternalAsync(List<QueryHistoryEntry> entries)
    {
        try
        {
            var json = JsonSerializer.Serialize(entries, _jsonOptions);
            await File.WriteAllTextAsync(_historyFilePath, json);
        }
        catch (Exception)
        {
            // Intentionally swallow to avoid crashing the app on logging failure.
        }
    }
}
