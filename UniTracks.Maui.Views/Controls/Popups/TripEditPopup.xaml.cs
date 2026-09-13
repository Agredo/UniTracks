using CommunityToolkit.Maui.Views;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Maui.Views.Controls.Popups;

public partial class TripEditPopup : Popup
{
	private readonly TripEditPopupViewModel _viewModel;

	public TripEditPopup(TripEditPopupViewModel viewModel)
	{
		InitializeComponent();
		PopupCard.WidthRequest = PopupSizing.ContentWidth;

		BindingContext = viewModel;
		_viewModel = viewModel;

		_viewModel.Completed += OnCompleted;
	}

	private void OnCompleted(object? sender, TripEditResult? result)
	{
		_viewModel.Completed -= OnCompleted;
		MainThread.BeginInvokeOnMainThread(async () => await CloseAsync());
	}
}
