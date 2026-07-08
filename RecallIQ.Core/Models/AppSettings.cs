namespace RecallIQ.Core.Models;

public sealed class AppSettings
{
    public List<string> WatchedFolders { get; set; } = new();
    public string AiModelPath { get; set; } = "models/all-MiniLM-L6-v2.onnx";
    public string TesseractDataPath { get; set; } = "tessdata";
    public bool IsDarkMode { get; set; } = true;
    public bool IsIndexingEnabled { get; set; } = true;
    public int MaxConcurrentIndexingThreads { get; set; } = Environment.ProcessorCount;
    public int ChunkSizeTokens { get; set; } = 256;
    public int ChunkOverlapTokens { get; set; } = 32;
    public int MaxSearchResults { get; set; } = 20;
    public double MinRelevanceScore { get; set; } = 0.25;
    public string DatabasePath { get; set; } = "recalliq.db";
    public string LogDirectory { get; set; } = "logs";
    public int LogRetentionDays { get; set; } = 30;
}
