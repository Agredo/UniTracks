using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class TripComparePage : ContentPage
{
	public TripComparePage(TripComparePageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
