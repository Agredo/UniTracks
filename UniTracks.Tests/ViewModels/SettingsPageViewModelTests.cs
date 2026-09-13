using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Data;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Pages;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="SettingsPageViewModel"/>. The switch is the only way to draw the raw GPS
/// track, so it must survive the trip back to the map: the value is read on construction and every
/// flip is written straight through to the settings store. The background location section only
/// appears where the platform grants foreground and background separately, and its button must reach
/// the system settings page because Android 11+ no longer offers "always" in the dialog.
/// </summary>
public sealed class SettingsPageViewModelTests
{
    [Fact]
    public void Constructor_ReadsTheStoredValue()
    {
        var settings = new FakeTrackSmoothingSettings { IsEnabled = false };

        var viewModel = Create(settings);

        Assert.False(viewModel.TrackSmoothingEnabled);
        Assert.Contains("Rohdaten", viewModel.SmoothingStateText);
    }

    [Fact]
    public void IsSmoothingEnabledByDefault()
    {
        var viewModel = Create(new FakeTrackSmoothingSettings());

        Assert.True(viewModel.TrackSmoothingEnabled);
        Assert.Equal("Glättung aktiv", viewModel.SmoothingStateText);
    }

    [Fact]
    public void EveryFlip_IsPersisted()
    {
        var settings = new FakeTrackSmoothingSettings();
        var viewModel = Create(settings);

        viewModel.TrackSmoothingEnabled = false;
        Assert.False(settings.IsEnabled);

        viewModel.TrackSmoothingEnabled = true;
        Assert.True(settings.IsEnabled);
    }

    [Fact]
    public void FlippingTheSwitch_UpdatesTheStateText()
    {
        var viewModel = Create(new FakeTrackSmoothingSettings());

        viewModel.TrackSmoothingEnabled = false;

        Assert.Contains("Rohdaten", viewModel.SmoothingStateText);
    }

    [Fact]
    public async Task Refresh_ReadsTheAlwaysPermission()
    {
        var permissions = new FakePermissions { Status = PermissionStatus.Denied };
        var viewModel = Create(permissions: permissions, hasSeparateGrant: true);

        await viewModel.RefreshBackgroundLocationAsync();

        Assert.Equal(Permission.LocationAlways, Assert.Single(permissions.CheckedPermissions));
        Assert.True(viewModel.BackgroundLocationMissing);
        Assert.Contains("Nur beim Verwenden", viewModel.BackgroundLocationStateText);
    }

    [Fact]
    public async Task Refresh_ReportsAGrantedAlwaysPermission()
    {
        var permissions = new FakePermissions { Status = PermissionStatus.Granted };
        var viewModel = Create(permissions: permissions, hasSeparateGrant: true);

        await viewModel.RefreshBackgroundLocationAsync();

        Assert.False(viewModel.BackgroundLocationMissing);
        Assert.Contains("freigegeben", viewModel.BackgroundLocationStateText);
    }

    [Fact]
    public async Task Refresh_IsSkippedOnPlatformsWithoutASeparateGrant()
    {
        var permissions = new FakePermissions { Status = PermissionStatus.Denied };
        var viewModel = Create(permissions: permissions, hasSeparateGrant: false);

        await viewModel.RefreshBackgroundLocationAsync();

        Assert.False(viewModel.IsBackgroundLocationVisible);
        Assert.Empty(permissions.CheckedPermissions);
    }

    [Fact]
    public async Task OpenSettings_StaysInTheSystemDialogWhenTheUserGrantsIt()
    {
        var permissions = new FakePermissions { Status = PermissionStatus.Granted };
        var appSettings = new FakeAppSettings();
        var viewModel = Create(permissions: permissions, appSettings: appSettings, hasSeparateGrant: true);

        await viewModel.OpenBackgroundLocationSettingsCommand.ExecuteAsync(null);

        Assert.Empty(permissions.RequestedPermissions);
        Assert.False(appSettings.WasShown);
    }

    [Fact]
    public async Task OpenSettings_RequestsThenOpensTheSystemPageWhenAndroidAnswersWithoutADialog()
    {
        // Android 11+ returns "denied" from the request without showing anything, so the app's
        // settings page is the only place where "always" can be picked.
        var permissions = new FakePermissions { Status = PermissionStatus.Denied };
        var appSettings = new FakeAppSettings();
        var viewModel = Create(permissions: permissions, appSettings: appSettings, hasSeparateGrant: true);

        await viewModel.OpenBackgroundLocationSettingsCommand.ExecuteAsync(null);

        Assert.Equal(Permission.LocationAlways, Assert.Single(permissions.RequestedPermissions));
        Assert.True(appSettings.WasShown);
        Assert.True(viewModel.BackgroundLocationMissing);
    }

    [Fact]
    public async Task OpenSettings_RequestThatGrantsIt_DoesNotOpenTheSystemPage()
    {
        var permissions = new FakePermissions { Status = PermissionStatus.Denied };
        var appSettings = new FakeAppSettings();
        var viewModel = Create(permissions: permissions, appSettings: appSettings, hasSeparateGrant: true);

        // The user grants it in the system dialog: the fake then answers Granted for the request.
        permissions.StatusAfterRequest = PermissionStatus.Granted;
        await viewModel.OpenBackgroundLocationSettingsCommand.ExecuteAsync(null);

        Assert.False(appSettings.WasShown);
        Assert.Equal(Permission.LocationAlways, Assert.Single(permissions.RequestedPermissions));
    }

    [Fact]
    public void Constructor_ShowsTheStoredMapStyle()
    {
        var mapStyleSettings = new FakeMapStyleSettings
        {
            Style = UniTracks.Services.Settings.MapStyleKind.Topographic
        };

        var viewModel = Create(mapStyleSettings: mapStyleSettings);

        Assert.Equal(UniTracks.Services.Settings.MapStyleKind.Topographic, viewModel.SelectedMapStyle.Kind);
        Assert.Contains("Topo", viewModel.SelectedMapStyle.DisplayName);
        Assert.Contains("OpenTopoMap", viewModel.MapStyleAttributionHint);
    }

    [Fact]
    public void MapStyleOptions_CoverEveryStyle()
    {
        var viewModel = Create();

        Assert.Equal(
            UniTracks.Services.Settings.MapStyleCatalog.All,
            viewModel.MapStyleOptions.Select(option => option.Kind));
    }

    [Fact]
    public void PickingAStyle_IsPersisted()
    {
        var mapStyleSettings = new FakeMapStyleSettings();
        var viewModel = Create(mapStyleSettings: mapStyleSettings);

        viewModel.SelectedMapStyle = UniTracks.Services.Settings.MapStyleCatalog.Option(
            UniTracks.Services.Settings.MapStyleKind.Light);

        Assert.Equal(UniTracks.Services.Settings.MapStyleKind.Light, mapStyleSettings.Style);
        Assert.Contains("Carto", viewModel.SelectedMapStyle.DisplayName);
    }

    [Fact]
    public async Task ShareDatabase_SharesAnExportCopyWithoutTouchingTheLiveFile()
    {
        var maintenance = new FakeDatabaseMaintenance { ExportCopyPath = "test://export.db" };
        var fileSystem = new FakeFileSystem();
        var viewModel = Create(databaseMaintenance: maintenance, fileSystem: fileSystem);

        await viewModel.ShareDatabaseCommand.ExecuteAsync(null);

        var shared = Assert.Single(fileSystem.SharedFiles);
        Assert.Equal(["test://export.db"], shared.Files);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ShareDatabase_ExplainsWhenThereIsNoDatabaseYet()
    {
        var maintenance = new FakeDatabaseMaintenance { ExportCopyPath = null };
        var dialog = new FakeDialogService();
        var fileSystem = new FakeFileSystem();
        var viewModel = Create(databaseMaintenance: maintenance, fileSystem: fileSystem, dialogService: dialog);

        await viewModel.ShareDatabaseCommand.ExecuteAsync(null);

        Assert.Empty(fileSystem.SharedFiles);
        Assert.Contains("keine Datenbank", Assert.Single(dialog.Alerts).Message);
    }

    [Fact]
    public async Task SaveDatabaseCopy_WritesTheExportThroughTheSystemSaveDialog()
    {
        var maintenance = new FakeDatabaseMaintenance { ExportCopyPath = "test://app-data/UniTracks-2026-09-14-0900.db" };
        var export = new FakeFileExportService
        {
            Result = FileExportResult.Saved("/storage/emulated/0/Documents/UniTracks-2026-09-14-0900.db")
        };
        var dialog = new FakeDialogService();
        var viewModel = Create(databaseMaintenance: maintenance, fileExportService: export, dialogService: dialog);

        await viewModel.SaveDatabaseCopyCommand.ExecuteAsync(null);

        var request = Assert.Single(export.Requests);
        Assert.Equal("test://app-data/UniTracks-2026-09-14-0900.db", request.SourcePath);
        Assert.Equal("UniTracks-2026-09-14-0900.db", request.SuggestedFileName);

        Assert.Contains("gespeichert", Assert.Single(dialog.Alerts).Title);
        Assert.Contains("/storage/emulated/0/Documents", dialog.Alerts[0].Message);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task SaveDatabaseCopy_ExplainsWhenThereIsNoDatabaseYet()
    {
        var maintenance = new FakeDatabaseMaintenance { ExportCopyPath = null };
        var export = new FakeFileExportService();
        var dialog = new FakeDialogService();
        var viewModel = Create(databaseMaintenance: maintenance, fileExportService: export, dialogService: dialog);

        await viewModel.SaveDatabaseCopyCommand.ExecuteAsync(null);

        Assert.Empty(export.Requests);
        Assert.Contains("keine Datenbank", Assert.Single(dialog.Alerts).Message);
    }

    [Fact]
    public async Task SaveDatabaseCopy_StaysQuietWhenTheDialogIsClosed()
    {
        var maintenance = new FakeDatabaseMaintenance { ExportCopyPath = "test://export.db" };
        var export = new FakeFileExportService { Result = FileExportResult.Cancelled() };
        var dialog = new FakeDialogService();
        var viewModel = Create(databaseMaintenance: maintenance, fileExportService: export, dialogService: dialog);

        await viewModel.SaveDatabaseCopyCommand.ExecuteAsync(null);

        Assert.Empty(dialog.Alerts);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task SaveDatabaseCopy_ReportsAFailedWrite()
    {
        var maintenance = new FakeDatabaseMaintenance { ExportCopyPath = "test://export.db" };
        var export = new FakeFileExportService { Result = FileExportResult.Failed("Kein Speicherplatz.") };
        var dialog = new FakeDialogService();
        var viewModel = Create(databaseMaintenance: maintenance, fileExportService: export, dialogService: dialog);

        await viewModel.SaveDatabaseCopyCommand.ExecuteAsync(null);

        Assert.Contains("Kein Speicherplatz.", Assert.Single(dialog.Alerts).Message);
    }

    [Fact]
    public async Task ImportDatabase_StagesThePickedFileAndTellsTheUserToRestart()
    {
        var maintenance = new FakeDatabaseMaintenance();
        var fileSystem = new FakeFileSystem { PickFilePathResult = "test://picked.db" };
        var dialog = new FakeDialogService();
        var viewModel = Create(databaseMaintenance: maintenance, fileSystem: fileSystem, dialogService: dialog);

        await viewModel.ImportDatabaseCommand.ExecuteAsync(null);

        Assert.Equal(["test://picked.db"], maintenance.StagedImports);
        Assert.Contains("nächsten Start", Assert.Single(dialog.Alerts).Title + dialog.Alerts[0].Message);
    }

    [Fact]
    public async Task ImportDatabase_DoesNothingWhenThePickerIsCancelled()
    {
        var maintenance = new FakeDatabaseMaintenance();
        var dialog = new FakeDialogService();
        var viewModel = Create(
            databaseMaintenance: maintenance,
            fileSystem: new FakeFileSystem { PickFilePathResult = string.Empty },
            dialogService: dialog);

        await viewModel.ImportDatabaseCommand.ExecuteAsync(null);

        Assert.Empty(maintenance.StagedImports);
        Assert.Empty(dialog.Alerts);
    }

    [Fact]
    public async Task ImportDatabase_ReportsARejectedFile()
    {
        var maintenance = new FakeDatabaseMaintenance { StageImportError = "Das ist keine UniTracks-Datenbank." };
        var dialog = new FakeDialogService();
        var viewModel = Create(
            databaseMaintenance: maintenance,
            fileSystem: new FakeFileSystem { PickFilePathResult = "test://fremd.db" },
            dialogService: dialog);

        await viewModel.ImportDatabaseCommand.ExecuteAsync(null);

        Assert.Equal("Das ist keine UniTracks-Datenbank.", Assert.Single(dialog.Alerts).Message);
    }

    [Fact]
    public async Task ResetDatabase_AsksFirstAndStopsWhenTheUserCancels()
    {
        var maintenance = new FakeDatabaseMaintenance();
        var dialog = new FakeDialogService { ConfirmResult = false };
        var viewModel = Create(databaseMaintenance: maintenance, dialogService: dialog);

        await viewModel.ResetDatabaseCommand.ExecuteAsync(null);

        Assert.Single(dialog.Confirms);
        Assert.Equal(0, maintenance.StagedResets);
        Assert.Empty(dialog.Alerts);
    }

    [Fact]
    public async Task ResetDatabase_StagesTheResetAfterConfirmation()
    {
        var maintenance = new FakeDatabaseMaintenance();
        var dialog = new FakeDialogService { ConfirmResult = true };
        var viewModel = Create(databaseMaintenance: maintenance, dialogService: dialog);

        await viewModel.ResetDatabaseCommand.ExecuteAsync(null);

        Assert.Equal(1, maintenance.StagedResets);
        Assert.Contains("Löschen vorgemerkt", Assert.Single(dialog.Alerts).Title);
    }

    [Fact]
    public void RefreshDatabaseStatus_ReReadsThePendingOperation()
    {
        var maintenance = new FakeDatabaseMaintenance();
        var viewModel = Create(databaseMaintenance: maintenance);

        Assert.False(viewModel.HasPendingDatabaseOperation);
        Assert.Empty(viewModel.LastImportText);

        maintenance.HasPendingOperation = true;
        maintenance.PendingOperationText = "Import beim nächsten Start";
        maintenance.LastImport = new DateTimeOffset(2024, 5, 6, 7, 8, 0, TimeSpan.Zero);
        viewModel.RefreshDatabaseStatus();

        Assert.True(viewModel.HasPendingDatabaseOperation);
        Assert.Equal("Import beim nächsten Start", viewModel.PendingDatabaseOperationText);
        Assert.Contains("Letzter Import", viewModel.LastImportText);
    }

    [Theory]
    [InlineData(0, "noch keine Datei")]
    [InlineData(900, "900 B")]
    [InlineData(2048, "2,0 KB")]
    [InlineData(3 * 1024 * 1024, "3,0 MB")]
    public void DatabaseInfo_FormatsTheSize(long bytes, string expected)
    {
        var maintenance = new FakeDatabaseMaintenance
        {
            DatabaseSizeBytes = bytes,
            DatabaseFileName = "unitracks.db"
        };

        var viewModel = Create(databaseMaintenance: maintenance);

        Assert.Contains(expected, viewModel.DatabaseInfoText);
    }

    [Theory]
    [InlineData("HelpPage")]
    [InlineData("AboutPage")]
    public async Task AppButtons_NavigateToTheirPage(string route)
    {
        var navigation = new FakeNavigationService();
        var viewModel = Create(navigation: navigation);

        var command = route == "HelpPage" ? viewModel.OpenHelpCommand : viewModel.OpenAboutCommand;

        await command.ExecuteAsync(null);

        Assert.Equal(route, Assert.Single(navigation.Navigations).Route);
    }

    /// <summary>
    /// "Was ist neu" shows the notes again even though the app already marked them as seen on this
    /// version - the button exists exactly to read them a second time.
    /// </summary>
    [Fact]
    public async Task WhatsNewButton_ShowsTheReleaseNotesAgain()
    {
        var changelog = new FakeChangelogPresenter();
        var viewModel = Create(changelogPresenter: changelog);

        await viewModel.ShowWhatsNewCommand.ExecuteAsync(null);

        Assert.Equal(1, changelog.Shown);
    }

    private static SettingsPageViewModel Create(
        FakeTrackSmoothingSettings? settings = null,
        FakePermissions? permissions = null,
        FakeAppSettings? appSettings = null,
        bool hasSeparateGrant = false,
        FakeMapStyleSettings? mapStyleSettings = null,
        FakeDatabaseMaintenance? databaseMaintenance = null,
        FakeFileSystem? fileSystem = null,
        FakeFileExportService? fileExportService = null,
        FakeDialogService? dialogService = null,
        FakeNavigationService? navigation = null,
        FakeChangelogPresenter? changelogPresenter = null) =>
        new(
            settings ?? new FakeTrackSmoothingSettings(),
            permissions ?? new FakePermissions(),
            appSettings ?? new FakeAppSettings(),
            mapStyleSettings ?? new FakeMapStyleSettings(),
            databaseMaintenance ?? new FakeDatabaseMaintenance(),
            fileSystem ?? new FakeFileSystem(),
            fileExportService ?? new FakeFileExportService(),
            navigation ?? new FakeNavigationService(),
            dialogService ?? new FakeDialogService(),
            changelogPresenter ?? new FakeChangelogPresenter())
        {
            HasSeparateBackgroundLocationGrant = hasSeparateGrant
        };
}
