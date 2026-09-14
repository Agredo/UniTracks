using CommunityToolkit.Maui.Views;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Controls.Popups;

public partial class TripFilterPopup : Popup
{
	private readonly TripFilterPopupViewModel _viewModel;

	public TripFilterPopup(TripFilterPopupViewModel viewModel)
	{
		InitializeComponent();
		PopupCard.WidthRequest = PopupSizing.ContentWidth;

		BindingContext = viewModel;
		_viewModel = viewModel;

		_viewModel.Completed += OnCompleted;
	}

	private void OnCompleted(object? sender, TripFilters? result)
	{
		_viewModel.Completed -= OnCompleted;
		MainThread.BeginInvokeOnMainThread(async () => await CloseAsync());
	}
}
