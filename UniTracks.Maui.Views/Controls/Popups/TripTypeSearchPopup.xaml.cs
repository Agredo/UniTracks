using CommunityToolkit.Maui.Views;
using UniTracks.Models.Trip;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Maui.Views.Controls.Popups;

public partial class TripTypeSearchPopup : Popup
{
	private readonly TripTypeSearchPopupViewModel _viewModel;

	public TripTypeSearchPopup(TripTypeSearchPopupViewModel viewModel)
	{
		InitializeComponent();
		PopupCard.WidthRequest = PopupSizing.ContentWidth;

		BindingContext = viewModel;
		_viewModel = viewModel;

		_viewModel.Completed += OnCompleted;
	}

	private void OnCompleted(object? sender, TripType? type)
	{
		_viewModel.Completed -= OnCompleted;
		MainThread.BeginInvokeOnMainThread(async () => await CloseAsync());
	}

	private void OnTypeTapped(object? sender, TappedEventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is TripType type)
		{
			_viewModel.SelectType(type);
		}
	}
}
