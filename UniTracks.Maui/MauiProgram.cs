using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using DrawnUi.Maui.Draw;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Shiny;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UniTracks.Maui.Services.Location;
using UniTracks.Services.Location;
using UniTracks.ViewModels;

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

            services.AddGps<Services.Location.GpsDelegate>();

            //Pages
            services.AddTransient<MainPage, MainPageViewModel>();

            //Services
            services.AddSingleton<ILocationService, LocationService>();




#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
