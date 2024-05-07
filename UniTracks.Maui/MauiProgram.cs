using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using DrawnUi.Maui.Draw;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Shiny;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UniTracks.Core.Services;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Maui.Services.Location;
using UniTracks.Services.ApplicationModel.DataTransfer;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using UniTracks.ViewModels;
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
                .UseDrawnUi()
                .UseSkiaSharp(true)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
#if IOS
            services.AddGps< Services.Location.GpsDelegate>();
#endif
            //Pages
            services.AddTransient<MainPage, MainPageViewModel>();

            //DBContext
            services.AddDbContext<SqliteDBContext>();
            services.AddSingleton(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            //Services
            services.AddSingleton<UniTracks.Services.IO.IFileSystem, FileSystem>();
            services.AddSingleton<ILocationService, LocationService>();
            services.AddSingleton<IGpsDataStorageService, GpsDataStorageService>();
            services.AddSingleton<IShare, Share>();




#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
