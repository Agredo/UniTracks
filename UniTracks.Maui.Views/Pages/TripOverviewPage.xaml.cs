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

		// The trip list hands the trip over without its GPS points, so the map and the profile load
		// their track here.
		_ = viewModel.LoadTrackAsync();
    }
}