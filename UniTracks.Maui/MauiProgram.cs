using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.LifecycleEvents;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.Seeding;
using UniTracks.Data.SQLite;
using UniTracks.Maui.Services.Changelog;
using UniTracks.Maui.Services.Data;
using UniTracks.Maui.Services.Location;
using UniTracks.Maui.Services.Settings;
using UniTracks.Maui.Views;
using UniTracks.Maui.Views.Controls.Popups;
using UniTracks.Maui.Views.Pages;
using UniTracks.Maui.Views.Pages.Tabs;
using UniTracks.Games.BaseCamp.Persistence;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Models.Constants;
using UniTracks.Services.Changelog;
using UniTracks.Services.Data;
using UniTracks.Services.Feedback;
using UniTracks.Services.Game;
using UniTracks.Services.Location;
using UniTracks.Services.Settings;
using UniTracks.Services.Stats;
using UniTracks.ViewModels.Changelog;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.Pages;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppContext.SetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", true);

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
                {
                    Shape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(20, 20, 20, 20),
                        StrokeThickness = 0,
                        Stroke = new SolidColorBrush(Colors.Transparent),
                    },
                });

                // The toolkit paints every popup white and insets its content by 15, which showed up
                // as a bright frame around the rounded card. The card now forms the whole popup and
                // the margin keeps it clear of the screen edges.
                options.SetPopupDefaults(new DefaultPopupSettings
                {
                    BackgroundColor = Colors.Transparent,
                    Padding = new Thickness(0),
                    Margin = new Thickness(PopupSizing.SideMargin),
                });
            })
            .UseSkiaSharp()
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows => windows.OnWindowCreated(window =>
                {
                    void ApplyTitleBarColors()
                    {
                        if (window.AppWindow?.TitleBar is not { } titleBar)
                            return;

                        var surface = Windows.UI.Color.FromArgb(0xFF, 0x12, 0x1A, 0x14);   // SurfaceAlt (TabBar)
                        var accent = Windows.UI.Color.FromArgb(0xFF, 0x4D, 0xE7, 0x90);   // Accent
                        var text = Windows.UI.Color.FromArgb(0xFF, 0xF2, 0xF7, 0xF3);     // TextPrimary
                        var hover = Windows.UI.Color.FromArgb(0xFF, 0x1B, 0x3A, 0x29);    // AccentSoft

                        titleBar.BackgroundColor = surface;
                        titleBar.ForegroundColor = text;
                        titleBar.InactiveBackgroundColor = surface;
                        titleBar.InactiveForegroundColor = text;
                        titleBar.ButtonBackgroundColor = surface;
                        titleBar.ButtonForegroundColor = text;
                        titleBar.ButtonHoverBackgroundColor = hover;
                        titleBar.ButtonHoverForegroundColor = accent;
                        titleBar.ButtonPressedBackgroundColor = accent;
                        titleBar.ButtonPressedForegroundColor = surface;
                        titleBar.ButtonInactiveBackgroundColor = surface;
                        titleBar.ButtonInactiveForegroundColor = text;
                    }

                    ApplyTitleBarColors();
                    window.Activated += (_, _) => ApplyTitleBarColors();
                }));
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var services = builder.Services;

        RegisterAgredoServices(services);
        RegisterUniTracksServices(services);
        RegisterDataAccess(services);

        // Seeds the active repository (EF Core SQLite or LiteDB) with the TripType catalog when
        // empty. On iOS the store is LiteDB and receives its seed here; elsewhere it is a no-op.
        services.AddSingleton<DatabaseInitializer>();

        RegisterPages(services);
        RegisterPopups(services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // A staged import or reset from the settings has to run before anything opens the database:
        // both the repository and the initializer hold the file handle for the whole session.
        // Resolving only IDatabaseMaintenance here keeps that true - its factory needs the file
        // system and nothing else.
        try
        {
            var maintenance = app.Services.GetRequiredService<IDatabaseMaintenance>();
            var report = app.Services.GetRequiredService<StartupDatabaseReport>();
            report.Record(maintenance.ApplyPending());
        }
        catch (Exception exception)
        {
            CrashLog.Write($"Startup database operation failed: {exception}");
        }

        return app;
    }

    // Reads the app's display version (e.g. "0.2") automatically. On unpackaged Windows the
    // AppInfo display version falls back to the 4-part assembly version ("0.2.0.0"), so trailing
    // zero components are trimmed to match the <ApplicationDisplayVersion> in the csproj. This is
    // also called while the app is being built, so it must never throw.
    private static string GetDisplayVersion()
    {
        try
        {
            var parts = AppInfo.Current.VersionString.Split('.');
            var length = parts.Length;
            while (length > 2 && parts[length - 1] == "0")
                length--;
            return string.Join('.', parts.Take(length));
        }
        catch (Exception)
        {
            return UnknownDisplayVersion;
        }
    }

    private const string UnknownDisplayVersion = "0.0";

    private static void RegisterAgredoServices(IServiceCollection services)
    {
        // Navigation
        services.AddSingleton<AgredoApplication.MVVM.Services.Abstractions.Navigation.INavigationService, AgredoApplication.MVVM.Services.Maui.Navigation.NavigationService>();
        services.AddSingleton<AgredoApplication.MVVM.Services.Abstractions.Navigation.IPopupNavigationService, AgredoApplication.MVVM.Services.Maui.Navigation.PopupNavigationService>();

        // IO / Application / Devices
        services.AddSingleton<AgredoApplication.MVVM.Services.Abstractions.IO.IFileSystem, AgredoApplication.MVVM.Services.Maui.IO.FileSystem>();
        services.AddSingleton<AgredoApplication.MVVM.Services.Abstractions.Application.IMainThread, AgredoApplication.MVVM.Services.Maui.Application.MainThread>();
        services.AddSingleton<AgredoApplication.MVVM.Services.Abstractions.Devices.IGeolocation, AgredoApplication.MVVM.Services.Maui.Devices.Geolocation>();

        // UI / Dialogs
        services.AddSingleton<AgredoApplication.MVVM.Services.Abstractions.UI.IDialogService, AgredoApplication.MVVM.Services.Maui.UI.DialogService>();
    }

    private static void RegisterUniTracksServices(IServiceCollection services)
    {
        services.AddSingleton<ILocationService, LocationService>();
        services.AddSingleton<IGpsDataStorageService, GpsDataStorageService>();
        services.AddSingleton<IGamificationService, GamificationService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<TripDistanceRecalculator>();

        // Kopien der Datenbank in der Dateien-App ablegen ("Speichern unter").
        services.AddSingleton<IFileExportService, FileExportService>();

        // User preference: whether the map draws the smoothed track or the raw GPS points.
        services.AddSingleton<ITrackSmoothingSettings, PreferencesTrackSmoothingSettings>();

        // User preference: compact or full cards in the trip list.
        services.AddSingleton<ITripCardLayoutSettings, PreferencesTripCardLayoutSettings>();

        // User preference: which tile layer the maps use.
        services.AddSingleton<IMapStyleSettings, PreferencesMapStyleSettings>();

        // BugBear feedback (version is read automatically from the app's display version).
        services.AddSingleton<IFeedbackService>(_ => new FeedbackService(GetDisplayVersion()));

#if ANDROID
        services.AddSingleton<IBackgroundLocationController, BackgroundLocationController>();
#elif IOS || MACCATALYST
        services.AddSingleton<IBackgroundLocationController, BackgroundLocationController>();
#else
        services.AddSingleton<IBackgroundLocationController, BackgroundLocationController>();
#endif

        // Lock-screen controls of a running recording (Android: the foreground notification's
        // actions, iOS: the notification's category actions, desktop: none).
        services.AddSingleton<IRecordingRemoteControls, RecordingRemoteControls>();

        // Games: coin economy + city builder. CoinService doubles as the games-layer
        // activity-stats port; the city store adapts the games persistence port to IRepository.
        services.AddSingleton<ICoinService, CoinService>();
        services.AddSingleton<IActivityStatsSource>(sp => sp.GetRequiredService<ICoinService>());
        services.AddSingleton<ICityStore, CityStore>();
        services.AddSingleton<ICoinAccountService, CoinAccountService>();
        services.AddSingleton<ICityBuilderService, CityBuilderService>();
        services.AddSingleton<ITowerDefenseStore, TowerDefenseStore>();
        services.AddSingleton<ITowerDefenseService, TowerDefenseService>();
        services.AddSingleton<ICampStore, CampStore>();
        services.AddSingleton<IBaseCampService, BaseCampService>();
        services.AddSingleton<IGameCatalogService, GameCatalogService>();
        services.AddSingleton<UniTracks.Services.ApplicationModel.IPermissions, UniTracks.Maui.Services.ApplicationModel.Permissions>();
        services.AddSingleton<UniTracks.Services.ApplicationModel.IAppSettings, UniTracks.Maui.Services.ApplicationModel.AppSettings>();
        services.AddSingleton<UniTracks.Services.Dispatching.IDispatcher, UniTracks.Maui.Services.Dispatching.Dispatcher>();

        // Trip comparison: the trip-type catalog resolves the type ids, the fingerprint service keeps
        // one route summary per trip, and the similarity service is the entry point the compare pages
        // call. The backfill indexes trips recorded before the feature existed; it is a singleton so
        // the app starts it once and every page sees the same indexing state.
        services.AddSingleton<UniTracks.Services.Comparison.ITripTypeCatalog, UniTracks.Services.Comparison.TripTypeCatalog>();
        services.AddSingleton<UniTracks.Services.Comparison.ITripFingerprintService, UniTracks.Services.Comparison.TripFingerprintService>();
        services.AddSingleton<UniTracks.Services.Comparison.ITripFingerprintBackfill, UniTracks.Services.Comparison.TripFingerprintBackfill>();
        services.AddSingleton<UniTracks.Services.Comparison.ITripSimilarityService, UniTracks.Services.Comparison.TripSimilarityService>();

        // Release notes: the JSON catalog is read once, the last shown version is persisted, and the
        // presenter decides whether the "what's new" popup has to appear.
        services.AddSingleton<IChangelogService, ChangelogService>();
        services.AddSingleton<IChangelogState, PreferencesChangelogState>();
        services.AddSingleton<IChangelogPresenter>(sp => new ChangelogPresenter(
            sp.GetRequiredService<IChangelogService>(),
            sp.GetRequiredService<IChangelogState>(),
            sp.GetRequiredService<AgredoApplication.MVVM.Services.Abstractions.Navigation.IPopupNavigationService>(),
            GetDisplayVersion()));
    }

    private static void RegisterDataAccess(IServiceCollection services)
    {
#if IOS
        // On iOS the app runs on CoreCLR + ReadyToRun (IsDynamicCodeSupported=false), where EF
        // Core can neither build its model at runtime nor run Database.Migrate(). We therefore
        // back the repository with LiteDB (document store, embedded aggregates) on iOS only.
        services.AddSingleton<ILiteDatabase>(sp =>
        {
            var fileSystem = sp.GetRequiredService<AgredoApplication.MVVM.Services.Abstractions.IO.IFileSystem>();
            var databasePath = Path.Combine(fileSystem.AppDataDirectory, ApplicationConstants.LiteDBName);
            return new LiteDatabase(databasePath);
        });
        services.AddSingleton<IRepository, LiteDbRepository>();
        services.AddSingleton<IDatabaseMaintenance>(sp => new DatabaseMaintenance(
            sp.GetRequiredService<AgredoApplication.MVVM.Services.Abstractions.IO.IFileSystem>(),
            useLiteDatabase: true));
#else
        // Android, Mac Catalyst and Windows run on JIT, where EF Core can build its model at
        // runtime and execute Database.Migrate(), so SQLite + EF Core remains the store.
        services.AddSingleton<SqliteDBContext>(sp =>
        {
            var fileSystem = sp.GetRequiredService<AgredoApplication.MVVM.Services.Abstractions.IO.IFileSystem>();
            var databasePath = Path.Combine(fileSystem.AppDataDirectory, ApplicationConstants.SQliteDatabaseName);
            return new SqliteDBContext(databasePath);
        });
        services.AddSingleton<IRepository, EfRepository>();
        services.AddSingleton<IDatabaseMaintenance>(sp => new DatabaseMaintenance(
            sp.GetRequiredService<AgredoApplication.MVVM.Services.Abstractions.IO.IFileSystem>(),
            useLiteDatabase: false));
#endif
        services.AddSingleton<StartupDatabaseReport>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<TripTabPage, TripTabPageViewModel>();
        services.AddTransient<RecordTripTabPage, RecordTripTabPageViewModel>();
        services.AddTransient<UserPage, UserPagevViewModel>();
        services.AddTransient<AchievementsPage, AchievementsPageViewModel>();
        services.AddTransient<TripOverviewPage, TripOverviewViewModel>();
        services.AddTransient<TripChartsPage, TripChartsPageViewModel>();
        services.AddTransient<TripComparePage, TripComparePageViewModel>();
        services.AddTransient<TripComparisonPage, TripComparisonPageViewModel>();
        services.AddTransient<GameTabPage, GameTabPageViewModel>();
        services.AddTransient<CityBuilderPage, CityBuilderPageViewModel>();
        services.AddTransient<TowerDefensePage, TowerDefensePageViewModel>();
        services.AddTransient<BaseCampPage, BaseCampPageViewModel>();
        services.AddTransient<AboutPageViewModel>(_ => new AboutPageViewModel(GetDisplayVersion()));
        services.AddTransient<AboutPage>();
        services.AddTransient<FeedbackPageViewModel>();
        services.AddTransient<FeedbackPage>();
        services.AddTransient<StatisticsPage, StatisticsPageViewModel>();
        services.AddTransient<HelpPageViewModel>(_ => new HelpPageViewModel(GetDisplayVersion()));
        services.AddTransient<HelpPage>();
        services.AddTransient<SettingsPage, SettingsPageViewModel>();
        services.AddTransient<ProfilePage, ProfilePageViewModel>();
    }

    private static void RegisterPopups(IServiceCollection services)
    {
        services.AddTransientPopup<UserCreationPopup, UserCreationPopupViewModel>();
        services.AddTransient<UserCreationPopupViewModel>();
        services.AddKeyedTransient<Popup, UserCreationPopup>(typeof(UserCreationPopupViewModel));

        services.AddTransientPopup<TripTypeSearchPopup, TripTypeSearchPopupViewModel>();
        services.AddTransient<TripTypeSearchPopupViewModel>();
        services.AddKeyedTransient<Popup, TripTypeSearchPopup>(typeof(TripTypeSearchPopupViewModel));

        services.AddTransientPopup<TripEditPopup, TripEditPopupViewModel>();
        services.AddTransient<TripEditPopupViewModel>();
        services.AddKeyedTransient<Popup, TripEditPopup>(typeof(TripEditPopupViewModel));

        services.AddTransientPopup<WhatsNewPopup, WhatsNewPopupViewModel>();
        services.AddTransient<WhatsNewPopupViewModel>(sp => new WhatsNewPopupViewModel(
            sp.GetRequiredService<IChangelogService>(),
            GetDisplayVersion()));
        services.AddKeyedTransient<Popup, WhatsNewPopup>(typeof(WhatsNewPopupViewModel));
    }
}