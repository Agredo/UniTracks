using System.ComponentModel;

namespace UniTracks.Services.Navigation;

public interface IPopupNavigation
{
    Dictionary<string, object> Parameters { get; set; }

    void ShowPopup<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged;
    void ShowPopup<TViewModel>(Dictionary<string, object> parameters) where TViewModel : INotifyPropertyChanged;
    Task<object?> ShowPopupAsync<TViewModel>() where TViewModel : INotifyPropertyChanged;
    Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting, CancellationToken cancellationToken = default) where TViewModel : INotifyPropertyChanged;
    Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel, CancellationToken cancellationToken = default) where TViewModel : INotifyPropertyChanged;
}
