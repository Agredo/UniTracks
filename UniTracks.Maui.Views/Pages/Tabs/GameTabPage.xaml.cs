using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui.Views.Pages.Tabs;

public partial class GameTabPage : ContentPage
{
    private readonly GameTabPageViewModel viewModel;

    public GameTabPage(GameTabPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Coins may have been earned or spent while away from the tab.
        _ = viewModel.RefreshCommand.ExecuteAsync(null);
    }
}
