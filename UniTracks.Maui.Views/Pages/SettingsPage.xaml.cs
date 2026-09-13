using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class SettingsPage : ContentPage
{
	private readonly SettingsPageViewModel viewModel;

	public SettingsPage(SettingsPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		this.viewModel = viewModel;
	}

	/// <summary>
	/// The user may have changed the location permission on the system page and the database file
	/// can change while the page is off screen; re-read both on every appearance so the state shown
	/// is never stale.
	/// </summary>
	protected override async void OnAppearing()
	{
		base.OnAppearing();

		try
		{
			await viewModel.RefreshBackgroundLocationAsync();
		}
		catch (Exception ex)
		{
			// The settings page must never fail to appear because a permission could not be read.
			System.Diagnostics.Debug.WriteLine($"[UniTracks] SettingsPage.OnAppearing: {ex}");
		}

		viewModel.RefreshDatabaseStatus();
	}
}
