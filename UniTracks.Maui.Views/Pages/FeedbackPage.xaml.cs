using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class FeedbackPage : ContentPage
{
	public FeedbackPage(FeedbackPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnBugBearLinkTapped(object? sender, TappedEventArgs e)
	{
		await Launcher.OpenAsync("https://www.bug-bear.com");
	}
}
