using RecallIQ.Core.Events;
using RecallIQ.Core.Models;

namespace RecallIQ.Core.Interfaces;

public interface IIndexingService
{
    IndexingStatus CurrentStatus { get; }
    event EventHandler<IndexingProgressEventArgs>? ProgressChanged;
    Task StartIndexingAsync(IReadOnlyList<string> folderPaths, CancellationToken cancellationToken = default);
    Task StopIndexingAsync();
    Task RebuildIndexAsync(IReadOnlyList<string> folderPaths, CancellationToken cancellationToken = default);
    Task IndexSingleFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task RemoveFileFromIndexAsync(string filePath, CancellationToken cancellationToken = default);
}
