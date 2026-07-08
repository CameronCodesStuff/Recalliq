using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RecallIQ.UI.ViewModels;

namespace RecallIQ.UI.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
