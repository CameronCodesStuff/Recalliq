using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Models;
using RecallIQ.Storage;
using Xunit;

namespace RecallIQ.Tests.Storage;

public class SqliteStorageServiceTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteStorageService _storage;
    private readonly string _dbPath;

    public SqliteStorageServiceTests()
    {
        var logger = new Mock<ILogger<SqliteStorageService>>();
        _storage = new SqliteStorageService(logger.Object);
        _dbPath = Path.Combine(Path.GetTempPath(), $"recalliq_test_{Guid.NewGuid()}.db");
    }

    public async Task InitializeAsync()
    {
        await _storage.InitializeAsync(_dbPath);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _storage.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task InsertAndRetrieveDocument()
    {
        var doc = CreateTestDocument("/test/file.pdf");
        var id = await _storage.InsertDocumentAsync(doc);
        id.Should().BeGreaterThan(0);

        var retrieved = await _storage.GetDocumentByIdAsync(id);
        retrieved.Should().NotBeNull();
        retrieved!.FileName.Should().Be("file.pdf");
        retrieved.FilePath.Should().Be("/test/file.pdf");
    }

    [Fact]
    public async Task GetDocumentByPath_ReturnsCorrectDocument()
    {
        var doc = CreateTestDocument("/unique/path.txt");
        await _storage.InsertDocumentAsync(doc);

        var result = await _storage.GetDocumentByPathAsync("/unique/path.txt");
        result.Should().NotBeNull();
        result!.FilePath.Should().Be("/unique/path.txt");
    }

    [Fact]
    public async Task GetDocumentByPath_NonExistent_ReturnsNull()
    {
        var result = await _storage.GetDocumentByPathAsync("/no/such/file.txt");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocument_RemovesIt()
    {
        var doc = CreateTestDocument("/delete/me.pdf");
        var id = await _storage.InsertDocumentAsync(doc);

        await _storage.DeleteDocumentAsync(id);

        var result = await _storage.GetDocumentByIdAsync(id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task InsertAndRetrieveChunks()
    {
        var doc = CreateTestDocument("/chunk/test.pdf");
        var docId = await _storage.InsertDocumentAsync(doc);

        var chunks = new List<DocumentChunk>
        {
            new() { DocumentId = docId, ChunkIndex = 0, Content = "First chunk", Embedding = new byte[16], StartOffset = 0, EndOffset = 10 },
            new() { DocumentId = docId, ChunkIndex = 1, Content = "Second chunk", Embedding = new byte[16], StartOffset = 11, EndOffset = 22 }
        };
        await _storage.InsertChunksAsync(chunks);

        var retrieved = await _storage.GetChunksByDocumentIdAsync(docId);
        retrieved.Should().HaveCount(2);
        retrieved[0].Content.Should().Be("First chunk");
        retrieved[1].Content.Should().Be("Second chunk");
    }

    [Fact]
    public async Task DeleteChunksByDocumentId_RemovesAll()
    {
        var doc = CreateTestDocument("/chunk/delete.pdf");
        var docId = await _storage.InsertDocumentAsync(doc);

        var chunks = new List<DocumentChunk>
        {
            new() { DocumentId = docId, ChunkIndex = 0, Content = "Chunk", Embedding = new byte[16], StartOffset = 0, EndOffset = 5 }
        };
        await _storage.InsertChunksAsync(chunks);

        await _storage.DeleteChunksByDocumentIdAsync(docId);

        var result = await _storage.GetChunksByDocumentIdAsync(docId);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDocumentCount_ReturnsCorrectCount()
    {
        await _storage.InsertDocumentAsync(CreateTestDocument("/count/1.pdf"));
        await _storage.InsertDocumentAsync(CreateTestDocument("/count/2.pdf"));
        await _storage.InsertDocumentAsync(CreateTestDocument("/count/3.pdf"));

        var count = await _storage.GetDocumentCountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task InsertAndRetrieveActivity()
    {
        await _storage.InsertActivityAsync(new ActivityEntry
        {
            Type = ActivityType.FileIndexed,
            Message = "Test activity",
            FilePath = "/test/activity.pdf"
        });

        var activities = await _storage.GetRecentActivityAsync(10);
        activities.Should().ContainSingle();
        activities[0].Message.Should().Be("Test activity");
    }

    [Fact]
    public async Task ClearAllData_RemovesEverything()
    {
        await _storage.InsertDocumentAsync(CreateTestDocument("/clear/test.pdf"));
        await _storage.InsertActivityAsync(new ActivityEntry { Type = ActivityType.FileIndexed, Message = "test" });

        await _storage.ClearAllDataAsync();

        var docCount = await _storage.GetDocumentCountAsync();
        var activities = await _storage.GetRecentActivityAsync();
        docCount.Should().Be(0);
        activities.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateDocument_PersistsChanges()
    {
        var doc = CreateTestDocument("/update/test.pdf");
        var id = await _storage.InsertDocumentAsync(doc);

        var retrieved = await _storage.GetDocumentByIdAsync(id);
        retrieved!.ContentHash = "newhash123";
        retrieved.ChunkCount = 42;
        await _storage.UpdateDocumentAsync(retrieved);

        var updated = await _storage.GetDocumentByIdAsync(id);
        updated!.ContentHash.Should().Be("newhash123");
        updated.ChunkCount.Should().Be(42);
    }

    [Fact]
    public async Task GetDocumentCountByType_ReturnsGroupedCounts()
    {
        await _storage.InsertDocumentAsync(CreateTestDocument("/type/1.pdf", DocumentType.Pdf));
        await _storage.InsertDocumentAsync(CreateTestDocument("/type/2.pdf", DocumentType.Pdf));
        await _storage.InsertDocumentAsync(CreateTestDocument("/type/1.txt", DocumentType.Txt));

        var counts = await _storage.GetDocumentCountByTypeAsync();
        counts["Pdf"].Should().Be(2);
        counts["Txt"].Should().Be(1);
    }

    private static IndexedDocument CreateTestDocument(string path, DocumentType type = DocumentType.Pdf)
    {
        return new IndexedDocument
        {
            FilePath = path,
            FileName = Path.GetFileName(path),
            FileExtension = Path.GetExtension(path),
            DocumentType = type,
            FileSizeBytes = 1024,
            ContentHash = Guid.NewGuid().ToString("N"),
            LastModifiedUtc = DateTime.UtcNow,
            IndexedAtUtc = DateTime.UtcNow,
            ChunkCount = 1
        };
    }
}
