using Microsoft.Maui.ApplicationModel;
using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class AboutPage : ContentPage
{
	public AboutPage(AboutPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnLibraryTapped(object? sender, TappedEventArgs e)
	{
		if (sender is BindableObject bindable
			&& bindable.BindingContext is LibraryInfo library
			&& !string.IsNullOrWhiteSpace(library.GitHubUrl))
		{
			await Launcher.Default.OpenAsync(new Uri(library.GitHubUrl));
		}
	}
}
