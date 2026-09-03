using AgredoApplication.MVVM.Services.Abstractions.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class UserPagevViewModel : ObservableObject
{
    public IFileSystem FileSystem { get; }
    public IRepository Repository { get; }
    public string DatabasePath { get; }

    public UserPagevViewModel(IFileSystem fileSystem, IRepository repository)
    {
        FileSystem = fileSystem;
        Repository = repository;
        DatabasePath = repository.DatabasePath;
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

        await FileSystem.ShareFilesAsync("Share Database", new[] { DatabasePath });
    }
}
