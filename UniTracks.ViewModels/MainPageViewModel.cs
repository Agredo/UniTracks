using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Common.Contants;
using UniTracks.Services.ApplicationModel.DataTransfer;
using UniTracks.Services.IO;
using UniTracks.Services.Location;

namespace UniTracks.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public ILocationService LocationService { get; }
    public IShare Share { get; }
    public IFileSystem FileSystem { get; }

    public string DatabasePath { get; }
    public string LiteDBDatabasePath { get; private set; }

    public MainPageViewModel(ILocationService locationService, IShare share, IFileSystem fileSystem)
    {
        LocationService = locationService;
        Share = share;
        FileSystem = fileSystem;

        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.SQliteDatabaseName);
        LiteDBDatabasePath = Path.Combine(FileSystem.AppDataDirectory, ApplicationConstants.LiteDBName);

        StopListening();
    }

    [RelayCommand]
    public async Task StartListening()
    {
        await LocationService.StartListening();
    }

    [RelayCommand]
    public void StopListening()
    {
        LocationService.StopListening();
    }

    [RelayCommand]
    public async Task ShareDatabase()
    {

        await Share.ShareFiles("Share Databases", new string[] { DatabasePath, LiteDBDatabasePath});
    }
}
