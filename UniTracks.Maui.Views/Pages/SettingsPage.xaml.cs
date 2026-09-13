using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
