using UniTracks.Services.Navigation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;


namespace UniTracks.Maui.Services.Navigation;

public class PopupNavigation : IPopupNavigation
{
    public IPopupService PopupService { get; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    public PopupNavigation(IPopupService popupService)
    {
        PopupService = popupService;
    }

    public void ShowPopup<TViewModel>(Dictionary<string, object> parameters) where TViewModel : INotifyPropertyChanged
    {
        PopupService.ShowPopup<TViewModel>();
    }

    public void ShowPopup<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged
    {
        PopupService.ShowPopup<TViewModel>(onPresenting);
    }

    public async Task<object?> ShowPopupAsync<TViewModel>() where TViewModel : INotifyPropertyChanged
    {
        return await PopupService.ShowPopupAsync<TViewModel>();
    }

    public async Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel, CancellationToken cancellationToken = default) where TViewModel : INotifyPropertyChanged
    {
        return await PopupService.ShowPopupAsync<TViewModel>(viewModel, cancellationToken);
    }

    public async Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting, CancellationToken cancellationToken = default) where TViewModel : INotifyPropertyChanged
    {
        return await PopupService.ShowPopupAsync<TViewModel>(onPresenting, cancellationToken);
    }
}
