using System;
using System.Reactive;
using System.Threading.Tasks;
using Aictionary.Models;
using Aictionary.Services;
using ReactiveUI;

namespace Aictionary.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDictionaryService _dictionaryService;
    private readonly IOpenAIService _openAIService;

    private string _searchText = string.Empty;
    private WordDefinition? _currentDefinition;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public MainWindowViewModel(IDictionaryService dictionaryService, IOpenAIService openAIService)
    {
        _dictionaryService = dictionaryService;
        _openAIService = openAIService;

        SearchCommand = ReactiveCommand.CreateFromTask(
            SearchAsync,
            this.WhenAnyValue(x => x.SearchText, text => !string.IsNullOrWhiteSpace(text))
        );
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public WordDefinition? CurrentDefinition
    {
        get => _currentDefinition;
        set => this.RaiseAndSetIfChanged(ref _currentDefinition, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public ReactiveCommand<Unit, Unit> SearchCommand { get; }

    private async Task SearchAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        CurrentDefinition = null;

        try
        {
            // First, try to get from local cache
            var definition = await _dictionaryService.GetDefinitionAsync(SearchText);

            if (definition == null)
            {
                // If not found in cache, query OpenAI
                definition = await _openAIService.GenerateDefinitionAsync(SearchText);
            }

            if (definition != null)
            {
                CurrentDefinition = definition;
            }
            else
            {
                ErrorMessage = "Could not find or generate definition for this word.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
