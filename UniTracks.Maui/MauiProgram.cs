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
using UniTracks.Maui.Services.Location;
using UniTracks.Maui.Views.Controls.Popups;
using UniTracks.Maui.Views.Pages;
using UniTracks.Maui.Views.Pages.Tabs;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.Shared.Persistence;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Models.Constants;
using UniTracks.Services.Data;
using UniTracks.Services.Feedback;
using UniTracks.Services.Game;
using UniTracks.Services.Location;
using UniTracks.Services.Stats;
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

        return builder.Build();
    }

    // Reads the app's display version (e.g. "0.2") automatically. On unpackaged Windows the
    // AppInfo display version falls back to the 4-part assembly version ("0.2.0.0"), so trailing
    // zero components are trimmed to match the <ApplicationDisplayVersion> in the csproj.
    private static string GetDisplayVersion()
    {
        var parts = AppInfo.Current.VersionString.Split('.');
        var length = parts.Length;
        while (length > 2 && parts[length - 1] == "0")
            length--;
        return string.Join('.', parts.Take(length));
    }

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

        // BugBear feedback (version is read automatically from the app's display version).
        services.AddSingleton<IFeedbackService>(_ => new FeedbackService(GetDisplayVersion()));

#if ANDROID
        services.AddSingleton<IBackgroundLocationController, BackgroundLocationController>();
#elif IOS || MACCATALYST
        services.AddSingleton<IBackgroundLocationController, BackgroundLocationController>();
#else
        services.AddSingleton<IBackgroundLocationController, BackgroundLocationController>();
#endif

        // Games: coin economy + city builder. CoinService doubles as the games-layer
        // activity-stats port; the city store adapts the games persistence port to IRepository.
        services.AddSingleton<ICoinService, CoinService>();
        services.AddSingleton<IActivityStatsSource>(sp => sp.GetRequiredService<ICoinService>());
        services.AddSingleton<ICityStore, CityStore>();
        services.AddSingleton<ICityBuilderService, CityBuilderService>();
        services.AddSingleton<ITowerDefenseStore, TowerDefenseStore>();
        services.AddSingleton<ITowerDefenseService, TowerDefenseService>();
        services.AddSingleton<IGameCatalogService, GameCatalogService>();
        services.AddSingleton<UniTracks.Services.ApplicationModel.IPermissions, UniTracks.Maui.Services.ApplicationModel.Permissions>();
        services.AddSingleton<UniTracks.Services.Dispatching.IDispatcher, UniTracks.Maui.Services.Dispatching.Dispatcher>();
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
#endif
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<TripTabPage, TripTabPageViewModel>();
        services.AddTransient<RecordTripTabPage, RecordTripTabPageViewModel>();
        services.AddTransient<UserPage, UserPagevViewModel>();
        services.AddTransient<AchievementsPage, AchievementsPageViewModel>();
        services.AddTransient<TripOverviewPage, TripOverviewViewModel>();
        services.AddTransient<TripChartsPage, TripChartsPageViewModel>();
        services.AddTransient<GameTabPage, GameTabPageViewModel>();
        services.AddTransient<CityBuilderPage, CityBuilderPageViewModel>();
        services.AddTransient<TowerDefensePage, TowerDefensePageViewModel>();
        services.AddTransient<AboutPageViewModel>(_ => new AboutPageViewModel(GetDisplayVersion()));
        services.AddTransient<AboutPage>();
        services.AddTransient<FeedbackPageViewModel>();
        services.AddTransient<FeedbackPage>();
        services.AddTransient<StatisticsPage, StatisticsPageViewModel>();
    }

    private static void RegisterPopups(IServiceCollection services)
    {
        services.AddTransientPopup<UserCreationPopup, UserCreationPopupViewModel>();
        services.AddTransient<UserCreationPopupViewModel>();
        services.AddKeyedTransient<Popup, UserCreationPopup>(typeof(UserCreationPopupViewModel));

        services.AddTransientPopup<TripTypeSearchPopup, TripTypeSearchPopupViewModel>();
        services.AddTransient<TripTypeSearchPopupViewModel>();
        services.AddKeyedTransient<Popup, TripTypeSearchPopup>(typeof(TripTypeSearchPopupViewModel));
    }
}