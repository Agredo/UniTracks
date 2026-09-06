using UniTracks.Data.Seeding;
using UniTracks.Maui.Views;
using UniTracks.Maui.Views.Pages;

namespace UniTracks.Maui
{
    public partial class App : Application
    {
        public App(DatabaseInitializer databaseInitializer)
        {
            HookUnhandledExceptionLogging();

            InitializeComponent();

            // Seed the TripType catalog if the active repository is empty (relevant on iOS, where
            // the store is LiteDB and there is no EF Core HasData/migration seeding). The call
            // completes synchronously for both stores, so a short block here is safe.
            databaseInitializer.EnsureSeededAsync().GetAwaiter().GetResult();

            MainPage = new AppShell();

            Routing.RegisterRoute(nameof(TripOverviewPage), typeof(TripOverviewPage));
            Routing.RegisterRoute(nameof(CityBuilderPage), typeof(CityBuilderPage));
            Routing.RegisterRoute(nameof(TowerDefensePage), typeof(TowerDefensePage));
        }

        /// <summary>
        /// Captures managed exceptions that would otherwise surface as an opaque native
        /// stowed-exception in Windows Event Log (<c>0xc000027b</c>), so the true .NET
        /// cause is written to <see cref="CrashLog"/> and can be diagnosed.
        /// </summary>
        private static void HookUnhandledExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                CrashLog.Write($"AppDomain unhandled: {e.ExceptionObject}");

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                CrashLog.Write($"Unobserved task exception: {e.Exception}");
                e.SetObserved();
            };

#if WINDOWS
            Microsoft.UI.Xaml.Application.Current?.UnhandledException += (_, e) =>
                CrashLog.Write($"WinUI unhandled: {e.Exception}");
#endif
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
