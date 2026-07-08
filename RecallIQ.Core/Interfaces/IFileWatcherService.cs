using RecallIQ.Core.Events;

namespace RecallIQ.Core.Interfaces;

public interface IFileWatcherService : IDisposable
{
    event EventHandler<FileChangeEventArgs>? FileChanged;
    void WatchFolder(string folderPath);
    void UnwatchFolder(string folderPath);
    void UnwatchAll();
    IReadOnlyList<string> WatchedFolders { get; }
}
