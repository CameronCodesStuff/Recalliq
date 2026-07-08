using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;

namespace RecallIQ.Storage;

public sealed class SqliteStorageService : IStorageService
{
    private readonly ILogger<SqliteStorageService> _logger;
    private SqliteConnection? _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string _databasePath = string.Empty;
    private bool _disposed;

    public SqliteStorageService(ILogger<SqliteStorageService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        _databasePath = databasePath;
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connection = new SqliteConnection($"Data Source={databasePath}");
        await _connection.OpenAsync(cancellationToken);

        await ExecuteNonQueryAsync(@"
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;
            PRAGMA cache_size=10000;
            PRAGMA temp_store=MEMORY;
            PRAGMA mmap_size=268435456;
        ", cancellationToken);

        await CreateTablesAsync(cancellationToken);
        _logger.LogInformation("Database initialized at {Path}", databasePath);
    }

    private async Task CreateTablesAsync(CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(@"
            CREATE TABLE IF NOT EXISTS documents (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                file_path TEXT NOT NULL UNIQUE,
                file_name TEXT NOT NULL,
                file_extension TEXT NOT NULL,
                document_type INTEGER NOT NULL,
                file_size_bytes INTEGER NOT NULL,
                content_hash TEXT NOT NULL,
                last_modified_utc TEXT NOT NULL,
                indexed_at_utc TEXT NOT NULL,
                chunk_count INTEGER NOT NULL DEFAULT 0,
                is_ocr_processed INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_documents_path ON documents(file_path);
            CREATE INDEX IF NOT EXISTS idx_documents_hash ON documents(content_hash);
            CREATE INDEX IF NOT EXISTS idx_documents_type ON documents(document_type);

            CREATE TABLE IF NOT EXISTS chunks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                document_id INTEGER NOT NULL,
                chunk_index INTEGER NOT NULL,
                content TEXT NOT NULL,
                embedding BLOB NOT NULL,
                start_offset INTEGER NOT NULL,
                end_offset INTEGER NOT NULL,
                FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS idx_chunks_document ON chunks(document_id);

            CREATE TABLE IF NOT EXISTS activity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                type INTEGER NOT NULL,
                message TEXT NOT NULL,
                file_path TEXT,
                timestamp_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_activity_timestamp ON activity(timestamp_utc DESC);
            CREATE INDEX IF NOT EXISTS idx_activity_type ON activity(type);
        ", cancellationToken);
    }

    public async Task<IndexedDocument?> GetDocumentByPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand("SELECT * FROM documents WHERE file_path = @path");
            cmd.Parameters.AddWithValue("@path", filePath);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? MapDocument(reader) : null;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<IndexedDocument?> GetDocumentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand("SELECT * FROM documents WHERE id = @id");
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? MapDocument(reader) : null;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<long> InsertDocumentAsync(IndexedDocument document, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand(@"
                INSERT INTO documents (file_path, file_name, file_extension, document_type, file_size_bytes, content_hash, last_modified_utc, indexed_at_utc, chunk_count, is_ocr_processed)
                VALUES (@fp, @fn, @fe, @dt, @fs, @ch, @lm, @ia, @cc, @ocr);
                SELECT last_insert_rowid();");
            cmd.Parameters.AddWithValue("@fp", document.FilePath);
            cmd.Parameters.AddWithValue("@fn", document.FileName);
            cmd.Parameters.AddWithValue("@fe", document.FileExtension);
            cmd.Parameters.AddWithValue("@dt", (int)document.DocumentType);
            cmd.Parameters.AddWithValue("@fs", document.FileSizeBytes);
            cmd.Parameters.AddWithValue("@ch", document.ContentHash);
            cmd.Parameters.AddWithValue("@lm", document.LastModifiedUtc.ToString("O"));
            cmd.Parameters.AddWithValue("@ia", document.IndexedAtUtc.ToString("O"));
            cmd.Parameters.AddWithValue("@cc", document.ChunkCount);
            cmd.Parameters.AddWithValue("@ocr", document.IsOcrProcessed ? 1 : 0);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        finally { _semaphore.Release(); }
    }

    public async Task UpdateDocumentAsync(IndexedDocument document, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand(@"
                UPDATE documents SET file_name=@fn, file_extension=@fe, document_type=@dt,
                    file_size_bytes=@fs, content_hash=@ch, last_modified_utc=@lm,
                    indexed_at_utc=@ia, chunk_count=@cc, is_ocr_processed=@ocr
                WHERE id=@id");
            cmd.Parameters.AddWithValue("@id", document.Id);
            cmd.Parameters.AddWithValue("@fn", document.FileName);
            cmd.Parameters.AddWithValue("@fe", document.FileExtension);
            cmd.Parameters.AddWithValue("@dt", (int)document.DocumentType);
            cmd.Parameters.AddWithValue("@fs", document.FileSizeBytes);
            cmd.Parameters.AddWithValue("@ch", document.ContentHash);
            cmd.Parameters.AddWithValue("@lm", document.LastModifiedUtc.ToString("O"));
            cmd.Parameters.AddWithValue("@ia", document.IndexedAtUtc.ToString("O"));
            cmd.Parameters.AddWithValue("@cc", document.ChunkCount);
            cmd.Parameters.AddWithValue("@ocr", document.IsOcrProcessed ? 1 : 0);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _semaphore.Release(); }
    }

    public async Task DeleteDocumentAsync(long documentId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand("DELETE FROM documents WHERE id = @id");
            cmd.Parameters.AddWithValue("@id", documentId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _semaphore.Release(); }
    }

    public async Task DeleteDocumentByPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand("DELETE FROM documents WHERE file_path = @path");
            cmd.Parameters.AddWithValue("@path", filePath);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _semaphore.Release(); }
    }

    public async Task InsertChunksAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0) return;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var transaction = _connection!.BeginTransaction();
            foreach (var chunk in chunks)
            {
                using var cmd = CreateCommand(@"
                    INSERT INTO chunks (document_id, chunk_index, content, embedding, start_offset, end_offset)
                    VALUES (@did, @ci, @c, @e, @so, @eo)");
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@did", chunk.DocumentId);
                cmd.Parameters.AddWithValue("@ci", chunk.ChunkIndex);
                cmd.Parameters.AddWithValue("@c", chunk.Content);
                cmd.Parameters.AddWithValue("@e", chunk.Embedding);
                cmd.Parameters.AddWithValue("@so", chunk.StartOffset);
                cmd.Parameters.AddWithValue("@eo", chunk.EndOffset);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            transaction.Commit();
        }
        finally { _semaphore.Release(); }
    }

    public async Task DeleteChunksByDocumentIdAsync(long documentId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand("DELETE FROM chunks WHERE document_id = @id");
            cmd.Parameters.AddWithValue("@id", documentId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _semaphore.Release(); }
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var results = new List<DocumentChunk>();
            using var cmd = CreateCommand("SELECT * FROM chunks");
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(MapChunk(reader));
            }
            return results;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentIdAsync(long documentId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var results = new List<DocumentChunk>();
            using var cmd = CreateCommand("SELECT * FROM chunks WHERE document_id = @id ORDER BY chunk_index");
            cmd.Parameters.AddWithValue("@id", documentId);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(MapChunk(reader));
            }
            return results;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<long> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteScalarLongAsync("SELECT COUNT(*) FROM documents", cancellationToken);
    }

    public async Task<long> GetChunkCountAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteScalarLongAsync("SELECT COUNT(*) FROM chunks", cancellationToken);
    }

    public async Task<IReadOnlyList<IndexedDocument>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var results = new List<IndexedDocument>();
            using var cmd = CreateCommand("SELECT * FROM documents ORDER BY indexed_at_utc DESC");
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(MapDocument(reader));
            }
            return results;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_databasePath))
            return new FileInfo(_databasePath).Length;
        return await Task.FromResult(0L);
    }

    public async Task InsertActivityAsync(ActivityEntry entry, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand(@"
                INSERT INTO activity (type, message, file_path, timestamp_utc)
                VALUES (@t, @m, @fp, @ts)");
            cmd.Parameters.AddWithValue("@t", (int)entry.Type);
            cmd.Parameters.AddWithValue("@m", entry.Message);
            cmd.Parameters.AddWithValue("@fp", (object?)entry.FilePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ts", entry.TimestampUtc.ToString("O"));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _semaphore.Release(); }
    }

    public async Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var results = new List<ActivityEntry>();
            using var cmd = CreateCommand("SELECT * FROM activity ORDER BY timestamp_utc DESC LIMIT @c");
            cmd.Parameters.AddWithValue("@c", count);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new ActivityEntry
                {
                    Id = reader.GetInt64(0),
                    Type = (ActivityType)reader.GetInt32(1),
                    Message = reader.GetString(2),
                    FilePath = reader.IsDBNull(3) ? null : reader.GetString(3),
                    TimestampUtc = DateTime.Parse(reader.GetString(4))
                });
            }
            return results;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<long> GetActivityCountByTypeAsync(ActivityType type, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand("SELECT COUNT(*) FROM activity WHERE type = @t");
            cmd.Parameters.AddWithValue("@t", (int)type);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        finally { _semaphore.Release(); }
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await ExecuteNonQueryInternalAsync("DELETE FROM chunks; DELETE FROM documents; DELETE FROM activity; VACUUM;", cancellationToken);
            _logger.LogInformation("All data cleared");
        }
        finally { _semaphore.Release(); }
    }

    public async Task<Dictionary<string, int>> GetDocumentCountByTypeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var result = new Dictionary<string, int>();
            using var cmd = CreateCommand("SELECT document_type, COUNT(*) FROM documents GROUP BY document_type");
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var docType = (DocumentType)reader.GetInt32(0);
                result[docType.ToString()] = reader.GetInt32(1);
            }
            return result;
        }
        finally { _semaphore.Release(); }
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await ExecuteNonQueryInternalAsync(sql, cancellationToken);
        }
        finally { _semaphore.Release(); }
    }

    private async Task ExecuteNonQueryInternalAsync(string sql, CancellationToken cancellationToken)
    {
        using var cmd = CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<long> ExecuteScalarLongAsync(string sql, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var cmd = CreateCommand(sql);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        finally { _semaphore.Release(); }
    }

    private static IndexedDocument MapDocument(SqliteDataReader reader)
    {
        return new IndexedDocument
        {
            Id = reader.GetInt64(0),
            FilePath = reader.GetString(1),
            FileName = reader.GetString(2),
            FileExtension = reader.GetString(3),
            DocumentType = (DocumentType)reader.GetInt32(4),
            FileSizeBytes = reader.GetInt64(5),
            ContentHash = reader.GetString(6),
            LastModifiedUtc = DateTime.Parse(reader.GetString(7)),
            IndexedAtUtc = DateTime.Parse(reader.GetString(8)),
            ChunkCount = reader.GetInt32(9),
            IsOcrProcessed = reader.GetInt32(10) == 1
        };
    }

    private static DocumentChunk MapChunk(SqliteDataReader reader)
    {
        return new DocumentChunk
        {
            Id = reader.GetInt64(0),
            DocumentId = reader.GetInt64(1),
            ChunkIndex = reader.GetInt32(2),
            Content = reader.GetString(3),
            Embedding = (byte[])reader.GetValue(4),
            StartOffset = reader.GetInt32(5),
            EndOffset = reader.GetInt32(6)
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Close();
        _connection?.Dispose();
        _semaphore.Dispose();
    }
}
