namespace RecallIQ.Core.Events;

public sealed class FileChangeEventArgs : EventArgs
{
    public string FilePath { get; }
    public FileChangeType ChangeType { get; }

    public FileChangeEventArgs(string filePath, FileChangeType changeType)
    {
        FilePath = filePath;
        ChangeType = changeType;
    }
}

public enum FileChangeType
{
    Created,
    Modified,
    Deleted,
    Renamed
}
