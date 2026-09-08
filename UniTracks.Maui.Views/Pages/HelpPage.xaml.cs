using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class HelpPage : ContentPage
{
	public HelpPage(HelpPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
