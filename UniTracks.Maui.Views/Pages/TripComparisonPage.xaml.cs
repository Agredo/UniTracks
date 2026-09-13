using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class TripComparisonPage : ContentPage
{
    public TripComparisonPage(TripComparisonPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
