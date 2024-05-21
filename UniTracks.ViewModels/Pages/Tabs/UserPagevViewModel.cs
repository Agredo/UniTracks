using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.Location;
using UniTracks.Services.ApplicationModel.DataTransfer;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class UserPagevViewModel : ObservableObject
{
    public IShare Share { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public string DatabasePath { get; }

    public UserPagevViewModel(IShare share, IGenericRepository<SqliteDBContext> sqliteRepository)
    {
        Share = share;
        SqliteRepository = sqliteRepository;
        DatabasePath = sqliteRepository.Context.DatabasePath;
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
