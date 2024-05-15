using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Pages.Tabs;

public partial class RecordTripTabPage : ContentPage
{
	public RecordTripTabPage(RecordTripTabPageViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}