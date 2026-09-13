using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
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

    private static SettingsPageViewModel Create(
        FakeTrackSmoothingSettings? settings = null,
        FakePermissions? permissions = null,
        FakeAppSettings? appSettings = null,
        bool hasSeparateGrant = false) =>
        new(
            settings ?? new FakeTrackSmoothingSettings(),
            permissions ?? new FakePermissions(),
            appSettings ?? new FakeAppSettings())
        {
            HasSeparateBackgroundLocationGrant = hasSeparateGrant
        };
}
