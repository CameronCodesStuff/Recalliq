using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Events;
using RecallIQ.Core.Extensions;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;
using RecallIQ.Storage;

namespace RecallIQ.Indexing;

public sealed class IndexingService : IIndexingService
{
    private readonly ILogger<IndexingService> _logger;
    private readonly IStorageService _storage;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITextChunker _textChunker;
    private readonly DocumentParserFactory _parserFactory;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _indexLock = new(1, 1);

    public IndexingStatus CurrentStatus { get; } = new();
    public event EventHandler<IndexingProgressEventArgs>? ProgressChanged;

    public IndexingService(
        ILogger<IndexingService> logger,
        IStorageService storage,
        IEmbeddingService embeddingService,
        ITextChunker textChunker,
        DocumentParserFactory parserFactory)
    {
        _logger = logger;
        _storage = storage;
        _embeddingService = embeddingService;
        _textChunker = textChunker;
        _parserFactory = parserFactory;
    }

    public async Task StartIndexingAsync(IReadOnlyList<string> folderPaths, CancellationToken cancellationToken = default)
    {
        if (!await _indexLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("Indexing already in progress");
            return;
        }

        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CurrentStatus.State = IndexingState.Running;
            CurrentStatus.StartedAtUtc = DateTime.UtcNow;
            CurrentStatus.ProcessedFiles = 0;
            CurrentStatus.FailedFiles = 0;
            NotifyProgress();

            await _storage.InsertActivityAsync(new ActivityEntry
            {
                Type = ActivityType.IndexingStarted,
                Message = $"Indexing started for {folderPaths.Count} folder(s)"
            }, _cts.Token);

            var allFiles = new ConcurrentBag<string>();
            foreach (var folder in folderPaths)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var file in EnumerateSupportedFiles(folder))
                {
                    allFiles.Add(file);
                }
            }

            CurrentStatus.TotalFiles = allFiles.Count;
            NotifyProgress();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2),
                CancellationToken = _cts.Token
            };

            await Parallel.ForEachAsync(allFiles, parallelOptions, async (filePath, ct) =>
            {
                try
                {
                    await IndexSingleFileInternalAsync(filePath, ct);
                    Interlocked.Increment(ref CurrentStatus.ProcessedFiles);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to index {File}", filePath);
                    Interlocked.Increment(ref CurrentStatus.FailedFiles);
                }

                CurrentStatus.CurrentFile = filePath;
                NotifyProgress();
            });

            CurrentStatus.State = IndexingState.Idle;
            NotifyProgress();

            await _storage.InsertActivityAsync(new ActivityEntry
            {
                Type = ActivityType.IndexingStopped,
                Message = $"Indexing completed: {CurrentStatus.ProcessedFiles} processed, {CurrentStatus.FailedFiles} failed"
            }, CancellationToken.None);

            _logger.LogInformation("Indexing completed: {Processed}/{Total} files", CurrentStatus.ProcessedFiles, CurrentStatus.TotalFiles);
        }
        catch (OperationCanceledException)
        {
            CurrentStatus.State = IndexingState.Idle;
            _logger.LogInformation("Indexing cancelled");
        }
        finally
        {
            _indexLock.Release();
        }
    }

    public Task StopIndexingAsync()
    {
        _cts?.Cancel();
        CurrentStatus.State = IndexingState.Idle;
        NotifyProgress();
        return Task.CompletedTask;
    }

    public async Task RebuildIndexAsync(IReadOnlyList<string> folderPaths, CancellationToken cancellationToken = default)
    {
        CurrentStatus.State = IndexingState.Rebuilding;
        NotifyProgress();

        await _storage.ClearAllDataAsync(cancellationToken);

        await _storage.InsertActivityAsync(new ActivityEntry
        {
            Type = ActivityType.IndexRebuilt,
            Message = "Index rebuilt from scratch"
        }, cancellationToken);

        await StartIndexingAsync(folderPaths, cancellationToken);
    }

    public async Task IndexSingleFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            await IndexSingleFileInternalAsync(filePath, cancellationToken);
        }
        finally { _indexLock.Release(); }
    }

    private async Task IndexSingleFileInternalAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath)) return;

        var fileInfo = new FileInfo(filePath);
        var extension = fileInfo.Extension;
        var docType = DocumentTypeExtensions.FromExtension(extension);

        if (docType == DocumentType.Unknown) return;

        var contentHash = filePath.ComputeSha256();

        var existingDoc = await _storage.GetDocumentByPathAsync(filePath, cancellationToken);
        if (existingDoc != null && existingDoc.ContentHash == contentHash)
            return;

        var parser = _parserFactory.GetParser(filePath);
        if (parser == null) return;

        var text = await parser.ExtractTextAsync(filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(text)) return;

        if (existingDoc != null)
        {
            await _storage.DeleteChunksByDocumentIdAsync(existingDoc.Id, cancellationToken);
            await _storage.DeleteDocumentAsync(existingDoc.Id, cancellationToken);
        }

        var chunks = _textChunker.ChunkText(text);

        var document = new IndexedDocument
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileExtension = extension,
            DocumentType = docType,
            FileSizeBytes = fileInfo.Length,
            ContentHash = contentHash,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            IndexedAtUtc = DateTime.UtcNow,
            ChunkCount = chunks.Count,
            IsOcrProcessed = docType.IsImageType()
        };

        var docId = await _storage.InsertDocumentAsync(document, cancellationToken);

        var chunkTexts = chunks.Select(c => c.Text).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);

        var dbChunks = new List<DocumentChunk>();
        for (int i = 0; i < chunks.Count; i++)
        {
            dbChunks.Add(new DocumentChunk
            {
                DocumentId = docId,
                ChunkIndex = i,
                Content = chunks[i].Text,
                Embedding = VectorOperations.SerializeEmbedding(embeddings[i]),
                StartOffset = chunks[i].StartOffset,
                EndOffset = chunks[i].EndOffset
            });
        }

        await _storage.InsertChunksAsync(dbChunks, cancellationToken);

        var activityType = existingDoc != null ? ActivityType.FileUpdated : ActivityType.FileIndexed;
        await _storage.InsertActivityAsync(new ActivityEntry
        {
            Type = activityType,
            Message = $"{activityType}: {fileInfo.Name}",
            FilePath = filePath
        }, cancellationToken);
    }

    public async Task RemoveFileFromIndexAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var doc = await _storage.GetDocumentByPathAsync(filePath, cancellationToken);
        if (doc != null)
        {
            await _storage.DeleteChunksByDocumentIdAsync(doc.Id, cancellationToken);
            await _storage.DeleteDocumentAsync(doc.Id, cancellationToken);

            await _storage.InsertActivityAsync(new ActivityEntry
            {
                Type = ActivityType.FileRemoved,
                Message = $"Removed from index: {doc.FileName}",
                FilePath = filePath
            }, cancellationToken);
        }
    }

    private static IEnumerable<string> EnumerateSupportedFiles(string folder)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        foreach (var file in Directory.EnumerateFiles(folder, "*.*", options))
        {
            if (DocumentTypeExtensions.IsSupportedExtension(Path.GetExtension(file)))
                yield return file;
        }
    }

    private void NotifyProgress()
    {
        ProgressChanged?.Invoke(this, new IndexingProgressEventArgs(CurrentStatus));
    }
}
