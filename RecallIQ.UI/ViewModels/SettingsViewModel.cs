using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;

namespace RecallIQ.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IIndexingService _indexingService;
    private readonly IStorageService _storage;
    private readonly IFileWatcherService _fileWatcher;

    [ObservableProperty] private List<string> _watchedFolders = new();
    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _isIndexingEnabled;
    [ObservableProperty] private string _aiModelPath = string.Empty;
    [ObservableProperty] private int _maxSearchResults;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isRebuilding;
    [ObservableProperty] private string _selectedFolder = string.Empty;

    public SettingsViewModel(
        ISettingsService settings,
        IIndexingService indexingService,
        IStorageService storage,
        IFileWatcherService fileWatcher)
    {
        _settings = settings;
        _indexingService = indexingService;
        _storage = storage;
        _fileWatcher = fileWatcher;
    }

    [RelayCommand]
    private void LoadSettings()
    {
        var s = _settings.CurrentSettings;
        WatchedFolders = new List<string>(s.WatchedFolders);
        IsDarkMode = s.IsDarkMode;
        IsIndexingEnabled = s.IsIndexingEnabled;
        AiModelPath = s.AiModelPath;
        MaxSearchResults = s.MaxSearchResults;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var s = _settings.CurrentSettings;
        s.WatchedFolders = new List<string>(WatchedFolders);
        s.IsDarkMode = IsDarkMode;
        s.IsIndexingEnabled = IsIndexingEnabled;
        s.AiModelPath = AiModelPath;
        s.MaxSearchResults = MaxSearchResults;
        await _settings.SaveAsync();
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private void AddFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || WatchedFolders.Contains(path)) return;
        var updated = new List<string>(WatchedFolders) { path };
        WatchedFolders = updated;
        _fileWatcher.WatchFolder(path);
    }

    [RelayCommand]
    private void RemoveFolder()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolder)) return;
        var updated = new List<string>(WatchedFolders);
        updated.Remove(SelectedFolder);
        WatchedFolders = updated;
        _fileWatcher.UnwatchFolder(SelectedFolder);
    }

    [RelayCommand]
    private async Task RebuildIndexAsync()
    {
        IsRebuilding = true;
        StatusMessage = "Rebuilding index...";
        try
        {
            await _indexingService.RebuildIndexAsync(WatchedFolders);
            StatusMessage = "Index rebuilt successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rebuild failed: {ex.Message}";
        }
        finally
        {
            IsRebuilding = false;
        }
    }

    [RelayCommand]
    private async Task PauseIndexingAsync()
    {
        IsIndexingEnabled = !IsIndexingEnabled;
        if (!IsIndexingEnabled)
        {
            await _indexingService.StopIndexingAsync();
            StatusMessage = "Indexing paused.";
        }
        else
        {
            StatusMessage = "Indexing resumed.";
        }
    }
}
