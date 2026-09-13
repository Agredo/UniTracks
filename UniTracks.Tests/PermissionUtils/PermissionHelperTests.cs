using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.PermissionUtils;

namespace UniTracks.Tests.PermissionUtils;

/// <summary>
/// The recording asks for Android's location permission in two steps: "while in use" first, then
/// background access. Android 11+ answers the background request without any dialog, so the decision
/// which step is due has to hold on its own here — on iOS and on the test target the second step must
/// never run.
/// </summary>
public sealed class PermissionHelperTests
{
    [Theory]
    [InlineData(true, Permission.LocationWhenInUse)]
    [InlineData(false, Permission.LocationAlways)]
    public void PrimaryLocationPermission_DependsOnThePlatform(bool isAndroid, Permission expected)
    {
        Assert.Equal(expected, PermissionHelper.PrimaryLocationPermission(isAndroid));
    }

    [Theory]
    [InlineData(PermissionStatus.Granted, true)]
    [InlineData(PermissionStatus.Denied, false)]
    [InlineData(PermissionStatus.Unknown, false)]
    [InlineData(PermissionStatus.Restricted, false)]
    public void RequiresBackgroundLocationStep_OnAndroid_FollowsTheForegroundGrant(
        PermissionStatus primaryStatus,
        bool expected)
    {
        Assert.Equal(expected, PermissionHelper.RequiresBackgroundLocationStep(true, primaryStatus));
    }

    [Theory]
    [InlineData(PermissionStatus.Granted)]
    [InlineData(PermissionStatus.Denied)]
    public void RequiresBackgroundLocationStep_OutsideAndroid_IsNeverNeeded(PermissionStatus primaryStatus)
    {
        Assert.False(PermissionHelper.RequiresBackgroundLocationStep(false, primaryStatus));
    }

    [Fact]
    public async Task CheckAndRequestPermission_ReturnsTheStatusWithoutAskingWhenAlreadyGranted()
    {
        var permissions = new FakePermissions { Status = PermissionStatus.Granted };

        PermissionStatus status = await PermissionHelper.CheckAndRequestPermission(permissions, Permission.LocationWhenInUse);

        Assert.Equal(PermissionStatus.Granted, status);
        Assert.Empty(permissions.RequestedPermissions);
    }

    [Fact]
    public async Task CheckAndRequestPermission_AsksOnceWhenTheStatusIsNotGranted()
    {
        var permissions = new FakePermissions { Status = PermissionStatus.Denied };

        PermissionStatus status = await PermissionHelper.CheckAndRequestPermission(permissions, Permission.LocationWhenInUse);

        Assert.Equal(PermissionStatus.Denied, status);
        Assert.Equal(Permission.LocationWhenInUse, Assert.Single(permissions.RequestedPermissions));
    }
}
