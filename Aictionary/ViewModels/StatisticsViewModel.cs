using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Aictionary.Models;
using Aictionary.Services;
using ReactiveUI;

namespace Aictionary.ViewModels;

public class StatisticsViewModel : ViewModelBase
{
    private readonly IQueryHistoryService _historyService;

    private bool _isLoading;
    private string _emptyMessage = string.Empty;

    public StatisticsViewModel(IQueryHistoryService historyService)
    {
        _historyService = historyService;
    }

    public ObservableCollection<StatisticsGroupItemViewModel> DailyGroups { get; } = new();
    public ObservableCollection<StatisticsGroupItemViewModel> WeeklyGroups { get; } = new();
    public ObservableCollection<StatisticsGroupItemViewModel> MonthlyGroups { get; } = new();
    public ObservableCollection<StatisticsGroupItemViewModel> YearlyGroups { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string EmptyMessage
    {
        get => _emptyMessage;
        private set => this.RaiseAndSetIfChanged(ref _emptyMessage, value);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        DailyGroups.Clear();
        WeeklyGroups.Clear();
        MonthlyGroups.Clear();
        YearlyGroups.Clear();
        EmptyMessage = string.Empty;

        try
        {
            var entries = await _historyService.GetEntriesAsync();

            if (entries.Count == 0)
            {
                EmptyMessage = "No queries recorded yet.";
                return;
            }

            PopulateDailyGroups(entries);
            PopulateWeeklyGroups(entries);
            PopulateMonthlyGroups(entries);
            PopulateYearlyGroups(entries);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PopulateDailyGroups(IReadOnlyList<QueryHistoryEntry> entries)
    {
        PopulateGroups(
            entries,
            DailyGroups,
            localTime => localTime.Date,
            start => start.ToString("MMMM dd, yyyy"));
    }

    private void PopulateWeeklyGroups(IReadOnlyList<QueryHistoryEntry> entries)
    {
        PopulateGroups(
            entries,
            WeeklyGroups,
            GetStartOfWeek,
            start =>
            {
                var end = start.AddDays(6);
                return $"Week of {start:MMM dd} – {end:MMM dd, yyyy}";
            });
    }

    private void PopulateMonthlyGroups(IReadOnlyList<QueryHistoryEntry> entries)
    {
        PopulateGroups(
            entries,
            MonthlyGroups,
            localTime => new DateTime(localTime.Year, localTime.Month, 1),
            start => start.ToString("MMMM yyyy"));
    }

    private void PopulateYearlyGroups(IReadOnlyList<QueryHistoryEntry> entries)
    {
        PopulateGroups(
            entries,
            YearlyGroups,
            localTime => new DateTime(localTime.Year, 1, 1),
            start => start.ToString("yyyy"));
    }

    private void PopulateGroups(
        IReadOnlyList<QueryHistoryEntry> entries,
        ObservableCollection<StatisticsGroupItemViewModel> target,
        Func<DateTime, DateTime> periodStartSelector,
        Func<DateTime, string> periodLabelSelector)
    {
        var grouped = entries
            .Select(entry =>
            {
                var localTime = entry.QueriedAt.ToLocalTime();
                var start = periodStartSelector(localTime);
                var label = periodLabelSelector(start);
                return new
                {
                    Entry = entry,
                    LocalTime = localTime,
                    Start = start,
                    Label = label
                };
            })
            .GroupBy(x => x.Start)
            .OrderByDescending(g => g.Key);

        foreach (var group in grouped)
        {
            var wordGroups = group
                .GroupBy(x => x.Entry.Word, StringComparer.OrdinalIgnoreCase)
                .Select(gw =>
                {
                    var mostRecent = gw.Max(x => x.LocalTime);
                    var representativeWord = gw
                        .OrderByDescending(x => x.LocalTime)
                        .ThenBy(x => x.Entry.Word, StringComparer.OrdinalIgnoreCase)
                        .First()
                        .Entry.Word;
                    return new StatisticsWordCountViewModel(representativeWord, gw.Count(), mostRecent);
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Word, StringComparer.OrdinalIgnoreCase)
                .ToList();

            target.Add(new StatisticsGroupItemViewModel(
                group.First().Label,
                group.Count(),
                wordGroups));
        }
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var culture = CultureInfo.CurrentCulture;
        var diff = (7 + (date.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek)) % 7;
        return date.Date.AddDays(-diff);
    }

    public IEnumerable<string> GetAllWords()
    {
        var allWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in DailyGroups)
        {
            foreach (var wordCount in group.WordCounts)
            {
                allWords.Add(wordCount.Word);
            }
        }

        foreach (var group in WeeklyGroups)
        {
            foreach (var wordCount in group.WordCounts)
            {
                allWords.Add(wordCount.Word);
            }
        }

        foreach (var group in MonthlyGroups)
        {
            foreach (var wordCount in group.WordCounts)
            {
                allWords.Add(wordCount.Word);
            }
        }

        foreach (var group in YearlyGroups)
        {
            foreach (var wordCount in group.WordCounts)
            {
                allWords.Add(wordCount.Word);
            }
        }

        return allWords.OrderBy(w => w, StringComparer.OrdinalIgnoreCase);
    }

    public async Task RemoveWordAsync(string word)
    {
        await _historyService.RemoveWordEntriesAsync(word);
        await LoadAsync();
    }
}

public class StatisticsGroupItemViewModel
{
    public StatisticsGroupItemViewModel(string label, int totalQueries, IList<StatisticsWordCountViewModel> wordCounts)
    {
        Label = label;
        TotalQueries = totalQueries;
        WordCounts = new ReadOnlyCollection<StatisticsWordCountViewModel>(wordCounts);
    }

    public string Label { get; }
    public int TotalQueries { get; }
    public IReadOnlyList<StatisticsWordCountViewModel> WordCounts { get; }
}

public class StatisticsWordCountViewModel
{
    public StatisticsWordCountViewModel(string word, int count, DateTime lastQueriedAtLocal)
    {
        Word = word;
        Count = count;
        LastQueriedAtLocal = lastQueriedAtLocal;
    }

    public string Word { get; }
    public int Count { get; }
    public DateTime LastQueriedAtLocal { get; }
}
