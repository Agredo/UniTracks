using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Pages.Tabs;

public partial class AchievementsPage : ContentPage
{
    public AchievementsPage(AchievementsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
