using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RecallIQ.UI.ViewModels;

namespace RecallIQ.UI.Pages;

public sealed partial class DocumentsPage : Page
{
    public DocumentsViewModel ViewModel { get; }

    public DocumentsPage()
    {
        ViewModel = App.GetService<DocumentsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDocumentsCommand.ExecuteAsync(null);
    }

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedDocument = (sender as ListView)?.SelectedItem as RecallIQ.Core.Models.IndexedDocument;
    }

    private void ListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        ViewModel.OpenFileCommand.Execute(null);
    }
}
