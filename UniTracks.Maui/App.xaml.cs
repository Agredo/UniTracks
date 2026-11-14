using UniTracks.Data.Maintenance;
using UniTracks.Data.Seeding;
using UniTracks.Maui.Views;
using UniTracks.Maui.Views.Pages;
using UniTracks.Services.Comparison;
using UniTracks.Services.Data;
using UniTracks.ViewModels.Changelog;

namespace UniTracks.Maui
{
    public partial class App : Application
    {
        private readonly IChangelogPresenter changelogPresenter;
        private readonly StartupDatabaseReport startupDatabaseReport;
        private readonly AgredoApplication.MVVM.Services.Abstractions.UI.IDialogService dialogService;
        private readonly UniTracks.Services.Data.TripDistanceRecalculator distanceRecalculator;
        private readonly ITripFingerprintBackfill fingerprintBackfill;
        private readonly IReadOnlyList<IDataMigration> dataMigrations;

        public App(
            DatabaseInitializer databaseInitializer,
            UniTracks.Services.Data.TripDistanceRecalculator distanceRecalculator,
            ITripFingerprintBackfill fingerprintBackfill,
            IEnumerable<IDataMigration> dataMigrations,
            IChangelogPresenter changelogPresenter,
            StartupDatabaseReport startupDatabaseReport,
            AgredoApplication.MVVM.Services.Abstractions.UI.IDialogService dialogService)
        {
            HookUnhandledExceptionLogging();

            InitializeComponent();

            this.changelogPresenter = changelogPresenter;
            this.startupDatabaseReport = startupDatabaseReport;
            this.dialogService = dialogService;
            this.distanceRecalculator = distanceRecalculator;
            this.fingerprintBackfill = fingerprintBackfill;
            this.dataMigrations = dataMigrations.ToList();

            // Seed the TripType catalog if the active repository is empty (relevant on iOS, where
            // the store is LiteDB and there is no EF Core HasData/migration seeding). The stores now
            // answer asynchronously, so EnsureSeededAsync must not depend on this thread's
            // synchronization context - it uses ConfigureAwait(false) throughout. Blocking here is
            // still safe and intentional: seeding is a single small query, and doing it before the
            // shell is created keeps the first page from rendering an empty catalog.
            databaseInitializer.EnsureSeededAsync().GetAwaiter().GetResult();

            // Everything that is not needed to draw the first page now waits for it. Until recently
            // the distance recalculation and the fingerprint indexing started right here, and the
            // trip list - the first thing the user sees - queued its own full scan behind them.
            var shell = new AppShell();

            // Subscribed before the shell becomes the main page: Loaded can fire as soon as the
            // platform view is attached, and the release notes must not miss that first event.
            shell.Loaded += OnShellLoaded;
            MainPage = shell;

            Routing.RegisterRoute(nameof(TripOverviewPage), typeof(TripOverviewPage));
            Routing.RegisterRoute(nameof(TripChartsPage), typeof(TripChartsPage));
            Routing.RegisterRoute(nameof(TripComparePage), typeof(TripComparePage));
            Routing.RegisterRoute(nameof(TripComparisonPage), typeof(TripComparisonPage));
            Routing.RegisterRoute(nameof(CityBuilderPage), typeof(CityBuilderPage));
            Routing.RegisterRoute(nameof(TowerDefensePage), typeof(TowerDefensePage));
        Routing.RegisterRoute(nameof(BaseCampPage), typeof(BaseCampPage));
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
            Routing.RegisterRoute(nameof(FeedbackPage), typeof(FeedbackPage));
            Routing.RegisterRoute(nameof(StatisticsPage), typeof(StatisticsPage));
            Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
        }

        /// <summary>
        /// Shows the release notes once the shell is on screen; a popup needs a loaded page to attach
        /// to, which is why this is not done in the constructor.
        /// </summary>
        private void OnShellLoaded(object? sender, EventArgs e)
        {
            if (sender is VisualElement element)
            {
                element.Loaded -= OnShellLoaded;
            }

            _ = ShowStartupNoticesAsync();
            _ = RunBackgroundJobsAsync();
        }

        /// <summary>
        /// One-off data migrations and the maintenance passes over the existing library, all started
        /// once the shell is on screen. A migration runs first: it changes where the GPS points are
        /// stored, and the passes below read exactly those.
        /// </summary>
        private async Task RunBackgroundJobsAsync()
        {
            // Loaded fires when the shell is attached, not when the first frame is up - the trip list
            // is still reading its first page at that moment. A migration moving a whole library of
            // GPS points would take the database for itself and push that read past the ten seconds
            // the iOS watchdog allows for a cold start, so it waits its turn.
            await Task.Delay(TimeSpan.FromSeconds(1.5));

            try
            {
                foreach (var migration in dataMigrations)
                {
                    await migration.RunAsync();
                }
            }
            catch (Exception exception)
            {
                // A failed migration must never take the app down; the data keeps working the way it
                // is, and the next start tries again.
                CrashLog.Write($"Data migration failed: {exception}");
            }

            // Recalculate legacy metrics so old average-speed samples and smoothed distances cannot
            // keep disagreeing with the splits and overview.
            _ = distanceRecalculator.RecalculateAsync();

            // Indexes every trip that predates the comparison feature. The compare page reports that
            // indexing is still running and shows whatever is already there.
            fingerprintBackfill.EnsureStarted();
        }

        private async Task ShowStartupNoticesAsync()
        {
            await ShowDatabaseOperationResultAsync();
            await ShowChangelogAsync();
        }

        /// <summary>
        /// Reports the import or reset that was staged in the settings and applied while the app was
        /// starting. Silent when nothing was pending - which is the normal case.
        /// </summary>
        private async Task ShowDatabaseOperationResultAsync()
        {
            var result = startupDatabaseReport.Result;
            if (result.Message is not { Length: > 0 } message)
            {
                return;
            }

            try
            {
                var title = result.Applied ? "Datenbank aktualisiert" : "Datenbank";
                var text = result.Applied
                    ? $"{message}\n\nDie App arbeitet jetzt mit diesen Daten."
                    : message;

                await dialogService.AlertAsync(title, text, "OK");
            }
            catch (Exception exception)
            {
                CrashLog.Write($"Startup database notice failed: {exception}");
            }
        }

        private async Task ShowChangelogAsync()
        {
            try
            {
                await changelogPresenter.ShowIfUnseenAsync();
            }
            catch (Exception exception)
            {
                // Never let the release notes take the app down; the version stays unrecorded, so
                // the next start simply tries again.
                CrashLog.Write($"Changelog popup failed: {exception}");
            }
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
