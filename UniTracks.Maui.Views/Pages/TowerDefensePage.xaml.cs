using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class TowerDefensePage : ContentPage
{
    public TowerDefensePage(TowerDefensePageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
