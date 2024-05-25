using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using DrawnUi.Maui.Draw;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Shiny;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UniTracks.Core.Services;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Maui.Services.Location;
using UniTracks.Maui.Services.Mapsui;
using UniTracks.Maui.Services.Navigation;
using UniTracks.Maui.Views.Controls.Popups;
using UniTracks.Maui.Views.Pages;
using UniTracks.Maui.Views.Pages.Tabs;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.Data;
using UniTracks.Services.Dispatching;
using UniTracks.Services.Location;
using UniTracks.Services.MapsUI;
using UniTracks.Services.Navigation;
using UniTracks.ViewModels;
using UniTracks.ViewModels.Controls.Popups;
using UniTracks.ViewModels.Pages;
using UniTracks.ViewModels.Pages.Tabs;
using FileSystem = UniTracks.Maui.Services.IO.FileSystem;
using IShare = UniTracks.Services.ApplicationModel.DataTransfer.IShare;
using Share = UniTracks.Maui.Services.ApplicationModel.DataTransfer.Share;

namespace UniTracks.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            AppContext.SetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", true);

            var builder = MauiApp.CreateBuilder();
            var services = builder.Services;

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMarkup()
                .UseMicrocharts()
                .UseShiny()
                .UseSkiaSharp(true)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseDrawnUi();
#if !WINDOWS
            services.AddGps<Services.Location.GpsDelegate>();
#endif
            //Pages
            services.AddTransient<MainPage, MainPageViewModel>();
            services.AddTransient<StartPage, StartPageViewModel>();
            services.AddTransient<TripOverviewPage, TripOverviewViewModel>();
            services.AddTransient<RecordTripTabPage, RecordTripTabPageViewModel>();
            services.AddTransient<TripTabPage, TripTabPageViewModel>();
            services.AddTransient<UserPage, UserPagevViewModel>();

            //Popups
            services.AddTransientPopup<UserCreationPopup, UserCreationPopupViewModel>();

            //DBContext
            services.AddDbContext<SqliteDBContext>();
            services.AddSingleton(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddSingleton<ILiteDatabase, LiteDatabase>();
            services.AddSingleton(typeof(IGenericLiteDBRepository<>), typeof(GenericLiteDBRepository<>));

            //Services
            services.AddSingleton<UniTracks.Services.IO.IFileSystem, FileSystem>();
            services.AddSingleton<ILocationService, LocationService>();
            services.AddSingleton<IGpsDataStorageService, GpsDataStorageService>();
            services.AddSingleton<IShare, Share>();
            services.AddSingleton<UniTracks.Services.Navigation.INavigation, ShellNavigation>();
            services.AddSingleton<INavigationRoutes, ShellNavigationRoutes>();
            services.AddSingleton<IPermissions, UniTracks.Maui.Services.ApplicationModel.Permissons>();
            services.AddSingleton<IMainThread, UniTracks.Maui.Services.ApplicationModel.MainThread>();
            services.AddSingleton<UniTracks.Services.Dispatching.IDispatcher, UniTracks.Maui.Services.Dispatching.Dispatcher>();
            services.AddSingleton<IPopupNavigation, PopupNavigation>();

            services.AddTransient<IMapRenderer, MapsUIRenderer>();




#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
