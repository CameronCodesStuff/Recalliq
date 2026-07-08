using RecallIQ.Core.Models;

namespace RecallIQ.Core.Interfaces;

public interface IStorageService : IDisposable
{
    Task InitializeAsync(string databasePath, CancellationToken cancellationToken = default);
    Task<IndexedDocument?> GetDocumentByPathAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IndexedDocument?> GetDocumentByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InsertDocumentAsync(IndexedDocument document, CancellationToken cancellationToken = default);
    Task UpdateDocumentAsync(IndexedDocument document, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(long documentId, CancellationToken cancellationToken = default);
    Task DeleteDocumentByPathAsync(string filePath, CancellationToken cancellationToken = default);
    Task InsertChunksAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task DeleteChunksByDocumentIdAsync(long documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentIdAsync(long documentId, CancellationToken cancellationToken = default);
    Task<long> GetDocumentCountAsync(CancellationToken cancellationToken = default);
    Task<long> GetChunkCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndexedDocument>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);
    Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default);
    Task InsertActivityAsync(ActivityEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<long> GetActivityCountByTypeAsync(Enums.ActivityType type, CancellationToken cancellationToken = default);
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetDocumentCountByTypeAsync(CancellationToken cancellationToken = default);
}
