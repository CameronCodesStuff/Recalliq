using RecallIQ.Core.Models;

namespace RecallIQ.Core.Events;

public sealed class IndexingProgressEventArgs : EventArgs
{
    public IndexingStatus Status { get; }

    public IndexingProgressEventArgs(IndexingStatus status)
    {
        Status = status;
    }
}
