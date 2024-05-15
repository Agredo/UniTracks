using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoCoordinatePortable;
using LiteDB;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Common.Contants;
using UniTracks.Common.ExtensionMethods;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Services.ApplicationModel.DataTransfer;
using UniTracks.Services.Data;
using UniTracks.Services.IO;
using UniTracks.Services.Location;
using UniTracks.Services.Navigation;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class RecordTripTabPageViewModel : ObservableObject
{
    public INavigation Navigation { get; }
    public INavigationRoutes NavigationRoutes { get; }
    public ILocationService LocationService { get; }
    public IShare Share { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public string DatabasePath { get; private set; }

    public RecordTripTabPageViewModel(INavigation navigation, INavigationRoutes navigationRoutes, ILocationService locationService, IShare share, IGenericRepository<SqliteDBContext> sqliteRepository)
    {   
        Navigation = navigation;
        NavigationRoutes = navigationRoutes;
        LocationService = locationService;
        Share = share;
        SqliteRepository = sqliteRepository;

        DatabasePath = sqliteRepository.Context.DatabasePath;

        StopListening().Await();
    }

    [RelayCommand]
    public async Task StartListening()
    {
        await LocationService.StartListening();
    }

    [RelayCommand]
    public async Task StopListening()
    {
        LocationService.StopListening();

    }

    [RelayCommand]
    public async Task ShareDatabase()
    {
        List<Models.Location.Location> sqliteLocations = (await SqliteRepository.GetAllAsync<Location>()).ToList();
        //List<Models.Location.Location> liteDBLocations = (await LiteDBRepository.GetAllAsync<Location>()).ToList();
        Console.WriteLine($"Total SQLite Locations: {sqliteLocations.Count}");
        //Console.WriteLine($"Total LiteDB Locations: {liteDBLocations.Count}");

        sqliteLocations.ForEach(async x => Console.WriteLine($"SQLite {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));
        //liteDBLocations.ForEach(async x => Console.WriteLine($"LiteDB {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));

        //await LocationfromLastTrip();

        await Share.ShareFiles("Share Databases", new string[] { DatabasePath });
    }
}
