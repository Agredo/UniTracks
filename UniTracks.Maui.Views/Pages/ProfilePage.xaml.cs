using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfilePageViewModel viewModel;

    public ProfilePage(ProfilePageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
    }

    /// <summary>The form is filled from the database while the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await viewModel.EnsureLoadedAsync();
        }
        catch (Exception ex)
        {
            // The page stays usable when the profile cannot be read; saving then creates one.
            System.Diagnostics.Debug.WriteLine($"[UniTracks] ProfilePage.OnAppearing: {ex}");
        }
    }
}
