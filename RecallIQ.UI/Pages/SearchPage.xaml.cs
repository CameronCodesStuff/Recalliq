using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RecallIQ.UI.ViewModels;
using Windows.System;

namespace RecallIQ.UI.Pages;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        ViewModel = App.GetService<SearchViewModel>();
        InitializeComponent();
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            await ViewModel.SearchCommand.ExecuteAsync(null);
        }
    }
}
