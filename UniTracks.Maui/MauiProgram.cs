using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using DrawnUi.Maui.Draw;
using Microcharts.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Locations;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UniTracks.Core.Services;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Maui.Services.IO;
using UniTracks.Maui.Services.Location;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using UniTracks.ViewModels;
using FileSystem = UniTracks.Maui.Services.IO.FileSystem;

namespace UniTracks.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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

            services.AddGps<UniTracks.Maui.Services.Location.GpsDelegate>();

            //Pages
            services.AddTransient<MainPage, MainPageViewModel>();

            //Services
            services.AddSingleton<ILocationService, LocationService>();
            services.AddSingleton<IGpsDataStorageService, GpsDataStorageService>();
            services.AddSingleton<UniTracks.Services.IO.IFileSystem, FileSystem>();

            //DBContext
            services.AddDbContext<SqliteDBContext>();
            services.AddSingleton(typeof(IGenericRepository<>), typeof(GenericRepository<>));




#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
