using RecallIQ.Core.Enums;

namespace RecallIQ.Core.Models;

public sealed class ActivityEntry
{
    public long Id { get; set; }
    public ActivityType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
