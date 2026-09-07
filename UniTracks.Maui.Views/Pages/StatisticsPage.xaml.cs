using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class StatisticsPage : ContentPage
{
    public StatisticsPage(StatisticsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
