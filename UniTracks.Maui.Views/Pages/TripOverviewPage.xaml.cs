using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class TripOverviewPage : ContentPage
{
	public TripOverviewPage(TripOverviewViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}