using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;

namespace RecallIQ.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private List<SearchResult> _results = new();
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _hasSearched;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private SearchResult? _selectedResult;

    public SearchViewModel(ISearchService searchService, ISettingsService settings)
    {
        _searchService = searchService;
        _settings = settings;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        HasSearched = true;
        StatusMessage = "Searching...";
        try
        {
            var maxResults = _settings.CurrentSettings.MaxSearchResults;
            var minScore = _settings.CurrentSettings.MinRelevanceScore;
            var results = await _searchService.SearchAsync(SearchQuery, maxResults, minScore);
            Results = results.ToList();
            StatusMessage = Results.Count == 0
                ? "No results found."
                : $"Found {Results.Count} result(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void OpenFileLocation()
    {
        if (SelectedResult == null) return;
        var directory = Path.GetDirectoryName(SelectedResult.FilePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{SelectedResult.FilePath}\"",
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (SelectedResult == null || !File.Exists(SelectedResult.FilePath)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = SelectedResult.FilePath,
            UseShellExecute = true
        });
    }
}
