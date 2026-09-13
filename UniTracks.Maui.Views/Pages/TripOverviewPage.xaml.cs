using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class TripOverviewPage : ContentPage
{
	public TripOverviewPage(TripOverviewViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
		this.viewModel = viewModel;
    }

    private readonly TripOverviewViewModel viewModel;

    protected override void OnAppearing()
    {
		base.OnAppearing();

		// The settings page lives on the profile tab; coming back here must not keep showing a route
		// that was drawn with the previous smoothing setting.
		viewModel.RefreshSettings();
    }
}