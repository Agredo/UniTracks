using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class StartPage : ContentPage
{
	public StartPage(StartPageViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}