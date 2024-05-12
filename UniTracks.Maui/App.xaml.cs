using UniTracks.Maui.Views.Pages;

namespace UniTracks.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            Routing.RegisterRoute(nameof(TripOverviewPage), typeof(TripOverviewPage));
        }
    }
}
