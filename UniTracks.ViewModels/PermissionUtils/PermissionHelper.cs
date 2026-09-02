using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;

namespace UniTracks.ViewModels.PermissionUtils;

public static class PermissionHelper
{
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
