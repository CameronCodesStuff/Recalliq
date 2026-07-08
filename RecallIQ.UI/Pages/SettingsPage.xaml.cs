using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RecallIQ.UI.ViewModels;
using Windows.Storage.Pickers;

namespace RecallIQ.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.LoadSettingsCommand.Execute(null);
    }

    private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedFolder = (sender as ListView)?.SelectedItem as string ?? string.Empty;
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            ViewModel.AddFolderCommand.Execute(folder.Path);
        }
    }

    private void DarkMode_Toggled(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = ViewModel.IsDarkMode
                ? ElementTheme.Dark
                : ElementTheme.Light;
        }
    }
}
