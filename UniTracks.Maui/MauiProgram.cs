using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Maui.Services.Location;
using UniTracks.Maui.Views.Controls.Popups;
using UniTracks.Maui.Views.Pages;
using UniTracks.Maui.Views.Pages.Tabs;
using UniTracks.Models.Constants;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
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
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var services = builder.Services;

        RegisterAgredoServices(services);
        RegisterUniTracksServices(services);
        RegisterDataAccess(services);
        RegisterPages(services);
        RegisterPopups(services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
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
    }

    private static void RegisterUniTracksServices(IServiceCollection services)
    {
        services.AddSingleton<ILocationService, LocationService>();
        services.AddSingleton<IGpsDataStorageService, GpsDataStorageService>();
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
        services.AddTransient<TripOverviewPage, TripOverviewViewModel>();
    }

    private static void RegisterPopups(IServiceCollection services)
    {
        services.AddTransientPopup<UserCreationPopup, UserCreationPopupViewModel>();
        services.AddTransient<UserCreationPopupViewModel>();
        services.AddKeyedTransient<Popup, UserCreationPopup>(typeof(UserCreationPopupViewModel));
    }
}