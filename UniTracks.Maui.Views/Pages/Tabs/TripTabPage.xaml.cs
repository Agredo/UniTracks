using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Pages.Tabs;

public partial class TripTabPage : ContentPage
{
	public TripTabPage(TripTabPageViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}