using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RecallIQ.UI.Pages;
using RecallIQ.UI.Services;

namespace RecallIQ.UI;

public sealed partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;

    public MainWindow()
    {
        InitializeComponent();
        _navigationService = App.GetService<NavigationService>();
        _navigationService.Frame = ContentFrame;
        ExtendsContentIntoTitleBar = true;
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (NavView.MenuItems.Count > 0)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            var pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Search" => typeof(SearchPage),
                "Documents" => typeof(DocumentsPage),
                "Activity" => typeof(ActivityPage),
                "Settings" => typeof(SettingsPage),
                _ => typeof(DashboardPage)
            };
            _navigationService.Navigate(pageType);
        }
    }
}
