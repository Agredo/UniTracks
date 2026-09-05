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
            Routing.RegisterRoute(nameof(CityBuilderPage), typeof(CityBuilderPage));
            Routing.RegisterRoute(nameof(TowerDefensePage), typeof(TowerDefensePage));
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

#if WINDOWS
            window.Title = "UniTracks";
            window.TitleBar = new TitleBar
            {
                Title = "UniTracks",
                BackgroundColor = Color.FromArgb("#121A14"),
                ForegroundColor = Color.FromArgb("#F2F7F3")
            };
#endif

            return window;
        }
    }
}
