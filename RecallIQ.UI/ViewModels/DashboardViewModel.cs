using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;

namespace RecallIQ.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IStorageService _storage;
    private readonly ISettingsService _settings;

    [ObservableProperty] private long _totalDocuments;
    [ObservableProperty] private long _totalChunks;
    [ObservableProperty] private long _totalSearches;
    [ObservableProperty] private string _databaseSize = "0 B";
    [ObservableProperty] private int _watchedFolderCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private List<ActivityEntry> _recentActivity = new();
    [ObservableProperty] private Dictionary<string, int> _documentsByType = new();

    public DashboardViewModel(IStorageService storage, ISettingsService settings)
    {
        _storage = storage;
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            TotalDocuments = await _storage.GetDocumentCountAsync();
            TotalChunks = await _storage.GetChunkCountAsync();
            TotalSearches = await _storage.GetActivityCountByTypeAsync(Core.Enums.ActivityType.SearchPerformed);
            var dbSize = await _storage.GetDatabaseSizeAsync();
            DatabaseSize = Core.Extensions.StringExtensions.FormatFileSize(dbSize);
            WatchedFolderCount = _settings.CurrentSettings.WatchedFolders.Count;
            RecentActivity = (await _storage.GetRecentActivityAsync(10)).ToList();
            DocumentsByType = (await _storage.GetDocumentCountByTypeAsync());
        }
        finally
        {
            IsLoading = false;
        }
    }
}
