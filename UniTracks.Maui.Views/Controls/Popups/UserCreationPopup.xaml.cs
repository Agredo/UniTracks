using CommunityToolkit.Maui.Views;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Maui.Views.Controls.Popups;

public partial class UserCreationPopup : Popup
{
	public UserCreationPopup(UserCreationPopupViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;

        viewModel.OnClose += async result => await CloseAsync(result, CancellationToken.None);
    }
}