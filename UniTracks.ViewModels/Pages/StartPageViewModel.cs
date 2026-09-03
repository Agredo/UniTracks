using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages;

public partial class StartPageViewModel : ObservableObject
{
    public ILocationService LocationService { get; }
    public IFileSystem FileSystem { get; }
    public IGpsDataStorageService GpsDataStorageService { get; }
    public IRepository Repository { get; }
    public string DatabasePath { get; }

    [ObservableProperty]
    private ObservableCollection<LocationModel> locations = new ObservableCollection<LocationModel>();

    [ObservableProperty]
    private string? debugText;

    public StartPageViewModel(
        ILocationService locationService,
        IFileSystem fileSystem,
        IGpsDataStorageService gpsDataStorageService,
        IRepository repository)
    {
        LocationService = locationService;
        FileSystem = fileSystem;
        GpsDataStorageService = gpsDataStorageService;
        Repository = repository;
        DatabasePath = repository.DatabasePath;

        _ = StopListening();
    }

    [RelayCommand]
    private async Task StartListening()
    {
        await LocationService.StartListening();
    }

    [RelayCommand]
    private async Task StopListening()
    {
        LocationService.StopListening();

        await LoadLocationsFromLastTripAsync();
    }

    private async Task LoadLocationsFromLastTripAsync()
    {
        List<Trip> trips = (await Repository.GetAllAsync<Trip>(trip => trip.Locations)).ToList();

        if (trips.Count > 0)
        {
            Locations.Clear();
            Trip lastTrip = trips.Last();

            Console.WriteLine($"Last Trip: {lastTrip.ID} {lastTrip.StartTime}");
            lastTrip.Locations?.ForEach(location => Locations.Add(location));
        }
    }

    [RelayCommand]
    private async Task ShareDatabase()
    {
        List<Trip> trips = (await Repository.GetAllAsync<Trip>(trip => trip.Locations)).ToList();
        int locationCount = trips.Sum(t => t.Locations?.Count ?? 0);
        Console.WriteLine($"Total Trips: {trips.Count}");
        Console.WriteLine($"Total Locations: {locationCount}");

        trips.ForEach(t => t.Locations?.ForEach(
            x => Console.WriteLine($"{x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}")));

        await LoadLocationsFromLastTripAsync();

        await FileSystem.ShareFilesAsync("Share Database", new[] { DatabasePath });
    }

    [RelayCommand]
    private async Task ImportDatabase()
    {
        await Task.CompletedTask;
    }
}
