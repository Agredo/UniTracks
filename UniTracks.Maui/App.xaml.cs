using UniTracks.Data.Seeding;
using UniTracks.Maui.Views;
using UniTracks.Maui.Views.Pages;

namespace UniTracks.Maui
{
    public partial class App : Application
    {
        public App(DatabaseInitializer databaseInitializer, UniTracks.Services.Data.TripDistanceRecalculator distanceRecalculator)
        {
            HookUnhandledExceptionLogging();

            InitializeComponent();

            // Seed the TripType catalog if the active repository is empty (relevant on iOS, where
            // the store is LiteDB and there is no EF Core HasData/migration seeding). The stores now
            // answer asynchronously, so EnsureSeededAsync must not depend on this thread's
            // synchronization context - it uses ConfigureAwait(false) throughout. Blocking here is
            // still safe and intentional: seeding is a single small query, and doing it before the
            // shell is created keeps the first page from rendering an empty catalog.
            databaseInitializer.EnsureSeededAsync().GetAwaiter().GetResult();

            // Recalculate stored trip distances with the smoothing pipeline (trips recorded before
            // smoothing existed carry the noisy raw distance). Fire-and-forget so startup is not
            // blocked; already-recalculated trips are skipped, so later runs are cheap.
            _ = distanceRecalculator.RecalculateAsync();

            MainPage = new AppShell();

            Routing.RegisterRoute(nameof(TripOverviewPage), typeof(TripOverviewPage));
            Routing.RegisterRoute(nameof(TripChartsPage), typeof(TripChartsPage));
            Routing.RegisterRoute(nameof(CityBuilderPage), typeof(CityBuilderPage));
            Routing.RegisterRoute(nameof(TowerDefensePage), typeof(TowerDefensePage));
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
            Routing.RegisterRoute(nameof(FeedbackPage), typeof(FeedbackPage));
            Routing.RegisterRoute(nameof(StatisticsPage), typeof(StatisticsPage));
            Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
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
