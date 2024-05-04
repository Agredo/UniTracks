using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Services.Location;

namespace UniTracks.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public ILocationService LocationService { get; }
    public MainPageViewModel(ILocationService locationService)
    {
        LocationService = locationService;
    }

    [RelayCommand]
    public async Task StartListening()
    {
        await LocationService.StartListening(gpsInfo =>
        {
            var latituede = gpsInfo.Position.Latitude;
            var longitude = gpsInfo.Position.Longitude;
            Console.WriteLine($"Latitude: {latituede}, Longitude: {longitude}");
        });
    }

    [RelayCommand]
    public void StopListening()
    {
        LocationService.StopListening();
    }
}
