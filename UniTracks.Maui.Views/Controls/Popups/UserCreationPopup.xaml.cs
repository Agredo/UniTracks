using CommunityToolkit.Maui.Views;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Maui.Views.Controls.Popups;

public partial class UserCreationPopup : Popup
{
	private readonly UserCreationPopupViewModel _viewModel;

	public UserCreationPopup(UserCreationPopupViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
		_viewModel = viewModel;

		_viewModel.Completed += OnCompleted;
	}

	private void OnCompleted(object? sender, bool result)
	{
		_viewModel.Completed -= OnCompleted;
		MainThread.BeginInvokeOnMainThread(async () => await CloseAsync());
	}
}