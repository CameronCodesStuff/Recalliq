using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;

namespace RecallIQ.UI.ViewModels;

public partial class DocumentsViewModel : ObservableObject
{
    private readonly IStorageService _storage;

    [ObservableProperty] private List<IndexedDocument> _documents = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private IndexedDocument? _selectedDocument;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private List<IndexedDocument> _filteredDocuments = new();

    public DocumentsViewModel(IStorageService storage)
    {
        _storage = storage;
    }

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        IsLoading = true;
        try
        {
            var docs = await _storage.GetAllDocumentsAsync();
            Documents = docs.ToList();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            FilteredDocuments = new List<IndexedDocument>(Documents);
        }
        else
        {
            FilteredDocuments = Documents
                .Where(d => d.FileName.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                          || d.FilePath.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (SelectedDocument == null || !File.Exists(SelectedDocument.FilePath)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = SelectedDocument.FilePath,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void OpenFileLocation()
    {
        if (SelectedDocument == null) return;
        var dir = Path.GetDirectoryName(SelectedDocument.FilePath);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{SelectedDocument.FilePath}\"",
                UseShellExecute = true
            });
        }
    }
}
