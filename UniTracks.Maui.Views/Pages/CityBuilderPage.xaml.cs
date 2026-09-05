using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class CityBuilderPage : ContentPage
{
    public CityBuilderPage(CityBuilderPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
