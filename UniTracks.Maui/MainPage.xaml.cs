
using UniTracks.ViewModels;

namespace UniTracks.Maui;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel mainPageViewModel)
    {
        InitializeComponent();
        BindingContext = mainPageViewModel;   
    }
}
