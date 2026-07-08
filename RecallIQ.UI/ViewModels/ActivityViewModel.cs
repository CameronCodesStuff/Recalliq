using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;

namespace RecallIQ.UI.ViewModels;

public partial class ActivityViewModel : ObservableObject
{
    private readonly IStorageService _storage;

    [ObservableProperty] private List<ActivityEntry> _activities = new();
    [ObservableProperty] private bool _isLoading;

    public ActivityViewModel(IStorageService storage)
    {
        _storage = storage;
    }

    [RelayCommand]
    private async Task LoadActivitiesAsync()
    {
        IsLoading = true;
        try
        {
            var entries = await _storage.GetRecentActivityAsync(200);
            Activities = entries.ToList();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
