using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RecallIQ.Core.Events;
using RecallIQ.Core.Extensions;
using RecallIQ.Core.Interfaces;

namespace RecallIQ.Indexing;

public sealed class FileWatcherService : IFileWatcherService
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, DateTime> _debounceMap = new();
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);
    private bool _disposed;

    public event EventHandler<FileChangeEventArgs>? FileChanged;
    public IReadOnlyList<string> WatchedFolders => _watchers.Keys.ToList();

    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger;
    }

    public void WatchFolder(string folderPath)
    {
        if (_disposed) return;
        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Folder does not exist: {Path}", folderPath);
            return;
        }
        if (_watchers.ContainsKey(folderPath)) return;

        var watcher = new FileSystemWatcher(folderPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, e) => OnFileEvent(e.FullPath, FileChangeType.Created);
        watcher.Changed += (_, e) => OnFileEvent(e.FullPath, FileChangeType.Modified);
        watcher.Deleted += (_, e) => OnFileEvent(e.FullPath, FileChangeType.Deleted);
        watcher.Renamed += (_, e) =>
        {
            OnFileEvent(e.OldFullPath, FileChangeType.Deleted);
            OnFileEvent(e.FullPath, FileChangeType.Created);
        };
        watcher.Error += (_, e) => _logger.LogError(e.GetException(), "FileSystemWatcher error for {Path}", folderPath);

        _watchers[folderPath] = watcher;
        _logger.LogInformation("Watching folder: {Path}", folderPath);
    }

    private void OnFileEvent(string filePath, FileChangeType changeType)
    {
        if (!DocumentTypeExtensions.IsSupportedExtension(Path.GetExtension(filePath)))
            return;

        var key = $"{filePath}:{changeType}";
        var now = DateTime.UtcNow;

        if (_debounceMap.TryGetValue(key, out var lastEvent) && now - lastEvent < _debounceInterval)
            return;

        _debounceMap[key] = now;
        FileChanged?.Invoke(this, new FileChangeEventArgs(filePath, changeType));
    }

    public void UnwatchFolder(string folderPath)
    {
        if (_watchers.TryRemove(folderPath, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _logger.LogInformation("Unwatched folder: {Path}", folderPath);
        }
    }

    public void UnwatchAll()
    {
        foreach (var path in _watchers.Keys.ToList())
        {
            UnwatchFolder(path);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnwatchAll();
    }
}
