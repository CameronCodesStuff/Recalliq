namespace RecallIQ.Core.Models;

public sealed class DashboardStats
{
    public long TotalDocuments { get; set; }
    public long TotalChunks { get; set; }
    public long TotalSearches { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public int WatchedFolderCount { get; set; }
    public Dictionary<string, int> DocumentsByType { get; set; } = new();
    public List<ActivityEntry> RecentActivity { get; set; } = new();
}
