using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.IO;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.Constants;
using UniTracks.Models.Trip;
using UniTracks.Services.Data;
using UniTracks.Services.Location;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class TripTabPageViewModel : ObservableObject
{
    public INavigationService Navigation { get; }
    public IPopupNavigationService PopupNavigation { get; }
    public ILocationService LocationService { get; }
    public IFileSystem FileSystem { get; }
    public IGpsDataStorageService GpsDataStorageService { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public IGenericLiteDBRepository<ILiteDatabase> LiteDBRepository { get; }
    public string DatabasePath { get; }
    public string LiteDBDatabasePath { get; private set; }

    [ObservableProperty]
    private ObservableCollection<LocationModel> locations = new ObservableCollection<LocationModel>();

    [ObservableProperty]
    private ObservableCollection<Trip> trips = new ObservableCollection<Trip>();

    [ObservableProperty]
    private string? debugText;

    private Trip? selectedTrip;
    public Trip? SelectedTrip
    {
        get => selectedTrip;
        set
        {
            if (SetProperty(ref selectedTrip, value) && value is not null)
            {
                _ = Navigation.ShellNavigationTo("TripOverviewPage", new Dictionary<string, object> { { "parameter", value } });
            }
        }
    }

    [ObservableProperty]
    private bool refreshIndicatorVisible;

    public TripTabPageViewModel(
        INavigationService navigation,
        IPopupNavigationService popupNavigation,
        ILocationService locationService,
        IFileSystem fileSystem,
        IGpsDataStorageService gpsDataStorageService,
        IGenericRepository<SqliteDBContext> sqliteRepository,
        IGenericLiteDBRepository<ILiteDatabase> liteDBRepository)
    {
        Navigation = navigation;
        PopupNavigation = popupNavigation;
        LocationService = locationService;
        FileSystem = fileSystem;
        GpsDataStorageService = gpsDataStorageService;
        SqliteRepository = sqliteRepository;
        LiteDBRepository = liteDBRepository;
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.SQliteDatabaseName);
        LiteDBDatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.LiteDBName);

        _ = GetTrips();
    }

    private async Task GetTrips()
    {
        Trips.Clear();
        var orderedTrips = (await SqliteRepository.GetAllAsync<Trip>(trip => trip.Locations))
            .OrderByDescending(trip => trip.StartTime)
            .ToList();

        foreach (var trip in orderedTrips)
        {
            Trips.Add(trip);
        }

        if (Trips.Count > 0)
        {
            Locations.Clear();
            Trip lastTrip = Trips.Last();

            Console.WriteLine($"Last Trip: {lastTrip.ID} {lastTrip.StartTime}");
            lastTrip.Locations?.ForEach(location => Locations.Add(location));
        }
    }

    [RelayCommand]
    private void SelectedTripChanged()
    {
        // Selection is handled via the SelectedTrip property setter.
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await GetTrips();
        RefreshIndicatorVisible = false;
    }
}
