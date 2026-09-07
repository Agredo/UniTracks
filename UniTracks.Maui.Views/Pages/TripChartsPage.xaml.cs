using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class TripChartsPage : ContentPage
{
	public TripChartsPage(TripChartsPageViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}
