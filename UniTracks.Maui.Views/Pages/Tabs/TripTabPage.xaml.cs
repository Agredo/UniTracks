using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Pages.Tabs;

public partial class TripTabPage : ContentPage
{
	public TripTabPage(TripTabPageViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // The compact-card switch may have changed on the settings page while this page was hidden.
        (BindingContext as TripTabPageViewModel)?.RefreshLayoutSettings();
    }
}