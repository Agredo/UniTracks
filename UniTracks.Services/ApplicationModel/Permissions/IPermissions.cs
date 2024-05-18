using UniTracks.Services.ApplicationModel.Permissions;

namespace UniTracks.Services.ApplicationModel;

public interface IPermissions
{
    Task<PermissionStatus> CheckPermissionStatusAsync(Permission permission);
    Task<PermissionStatus> RequestPermissionAsync(Permission permission);
    bool ShouldShowRationale(Permission permission);
}
