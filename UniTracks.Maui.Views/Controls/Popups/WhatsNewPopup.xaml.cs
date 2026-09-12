using CommunityToolkit.Maui.Views;
using UniTracks.ViewModels.Controls.Popups;

namespace UniTracks.Maui.Views.Controls.Popups;

public partial class WhatsNewPopup : Popup
{
    private readonly WhatsNewPopupViewModel viewModel;

    public WhatsNewPopup(WhatsNewPopupViewModel viewModel)
    {
        InitializeComponent();
        PopupCard.WidthRequest = PopupSizing.ContentWidth;
        BindingContext = viewModel;
        this.viewModel = viewModel;
        this.viewModel.Completed += OnCompleted;
    }

    private void OnCompleted(object? sender, bool result)
    {
        viewModel.Completed -= OnCompleted;
        MainThread.BeginInvokeOnMainThread(async () => await CloseAsync());
    }
}
