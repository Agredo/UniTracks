using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using AgredoApplication.MVVM.Services.Abstractions.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Services.Feedback;

namespace UniTracks.ViewModels.Pages;

public record FeedbackCategoryItem(string DisplayName, FeedbackCategory Category);

public partial class FeedbackPageViewModel : ObservableObject
{
    private readonly IFeedbackService _feedbackService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    public IReadOnlyList<FeedbackCategoryItem> Categories { get; } =
    [
        new("UI",           FeedbackCategory.Ui),
        new("Allgemein",    FeedbackCategory.General),
        new("UX",           FeedbackCategory.Ux),
        new("Spiele",       FeedbackCategory.Games),
        new("Belohnungen",  FeedbackCategory.Rewards),
    ];

    [ObservableProperty]
    private FeedbackCategoryItem _selectedCategory;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    public FeedbackPageViewModel(
        IFeedbackService feedbackService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _feedbackService = feedbackService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _selectedCategory = Categories[0];
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Description))
            return;

        IsBusy = true;
        try
        {
            var email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            var success = await _feedbackService.SubmitFeedbackAsync(
                SelectedCategory.Category,
                Description.Trim(),
                email);

            if (success)
            {
                await _dialogService.AlertAsync(
                    "Danke!",
                    "Dein Feedback wurde erfolgreich übermittelt.",
                    "OK");
                await _navigationService.NavigateBack();
            }
            else
            {
                await _dialogService.AlertAsync(
                    "Fehler",
                    "Das Feedback konnte nicht übermittelt werden. Bitte versuche es später erneut.",
                    "OK");
            }
        }
        catch
        {
            await _dialogService.AlertAsync(
                "Fehler",
                "Es ist ein unerwarteter Fehler aufgetreten. Bitte überprüfe deine Internetverbindung.",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
