using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;

namespace UniTracks.ViewModels.PermissionUtils;

public static class PermissionHelper
{
    /// <summary>
    /// The permission the recording asks for first. Android distinguishes "while in use" from
    /// background access and only offers "while in use" in the runtime dialog, so it asks for that
    /// one first; iOS asks for the full authorization right away.
    /// </summary>
    public static Permission PrimaryLocationPermission(bool isAndroid) =>
        isAndroid ? Permission.LocationWhenInUse : Permission.LocationAlways;

    /// <summary>
    /// Whether a second request for background location makes sense after the first one. Only Android
    /// separates the two, and the platform ignores a background request as long as the foreground
    /// grant is missing.
    /// </summary>
    public static bool RequiresBackgroundLocationStep(bool isAndroid, PermissionStatus primaryLocationStatus) =>
        isAndroid && primaryLocationStatus is PermissionStatus.Granted;

    public static async Task<PermissionStatus> CheckAndRequestPermission(IPermissions permissions, Permission permission, string additionalInfoText = "")
    {
        PermissionStatus status = await permissions.CheckPermissionStatusAsync(permission);

        if (status == PermissionStatus.Granted)
            return status;

        if (permissions.ShouldShowRationale(permission))
        {
            // Prompt the user with additional information as to why the permission is needed
        }

        status = await permissions.RequestPermissionAsync(permission);

        return status;
    }
}
