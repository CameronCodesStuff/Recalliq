using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RecallIQ.UI.ViewModels;

namespace RecallIQ.UI.Pages;

public sealed partial class ActivityPage : Page
{
    public ActivityViewModel ViewModel { get; }

    public ActivityPage()
    {
        ViewModel = App.GetService<ActivityViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadActivitiesCommand.ExecuteAsync(null);
    }
}
