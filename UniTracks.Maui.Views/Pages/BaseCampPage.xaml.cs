using UniTracks.ViewModels.Pages;

namespace UniTracks.Maui.Views.Pages;

public partial class BaseCampPage : ContentPage
{
    public BaseCampPage(BaseCampPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
