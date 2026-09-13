using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Pages.Tabs;

public partial class UserPage : ContentPage
{
	private readonly UserPagevViewModel viewModel;

	public UserPage(UserPagevViewModel viewModel)
	{
		InitializeComponent();

        BindingContext = viewModel;
        this.viewModel = viewModel;
    }

	/// <summary>
	/// Re-reads the numbers in the hero on every appearance: a finished trip or a profile edit
	/// changes them while the tab is off screen.
	/// </summary>
	protected override async void OnAppearing()
	{
		base.OnAppearing();

		try
		{
			await viewModel.RefreshAsync();
		}
		catch (Exception ex)
		{
			// The profile tab must still appear when the statistics cannot be computed.
			System.Diagnostics.Debug.WriteLine($"[UniTracks] UserPage.OnAppearing: {ex}");
		}
	}
}