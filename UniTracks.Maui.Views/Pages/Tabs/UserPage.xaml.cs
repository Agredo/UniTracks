using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Pages.Tabs;

public partial class UserPage : ContentPage
{
	public UserPage(UserPagevViewModel viewModel)
	{
		InitializeComponent();

        BindingContext = viewModel;
    }
}