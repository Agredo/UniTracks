using AgredoApplication.MVVM.Services.Abstractions.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.ViewModels.Pages.Tabs;

public partial class UserPagevViewModel : ObservableObject
{
    public IFileSystem FileSystem { get; }
    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }
    public string DatabasePath { get; }

    public UserPagevViewModel(IFileSystem fileSystem, IGenericRepository<SqliteDBContext> sqliteRepository)
    {
        FileSystem = fileSystem;
        SqliteRepository = sqliteRepository;
        DatabasePath = sqliteRepository.Context.DatabasePath;
    }

    [RelayCommand]
    private async Task ShareDatabase()
    {
        List<LocationModel> sqliteLocations = (await SqliteRepository.GetAllAsync<LocationModel>()).ToList();
        Console.WriteLine($"Total SQLite Locations: {sqliteLocations.Count}");

        sqliteLocations.ForEach(x => Console.WriteLine($"SQLite {x.Timestamp} - {x.ID} - {x.Longitude} - {x.Latitude}"));

        await FileSystem.ShareFilesAsync("Share Databases", new[] { DatabasePath });
    }
}
