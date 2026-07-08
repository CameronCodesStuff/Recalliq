using RecallIQ.Core.Enums;

namespace RecallIQ.Core.Models;

public sealed class IndexingStatus
{
    public IndexingState State { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public double ProgressPercent => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100.0 : 0.0;
    public DateTime? StartedAtUtc { get; set; }
    public TimeSpan Elapsed => StartedAtUtc.HasValue ? DateTime.UtcNow - StartedAtUtc.Value : TimeSpan.Zero;
}
