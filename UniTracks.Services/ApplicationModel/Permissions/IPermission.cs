using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Services.ApplicationModel.Permissions;

/// <summary>
/// Represents a permission.
/// </summary>
public interface IPermission
{
    /// <summary>
    /// Retrieves the current status of this permission.
    /// </summary>
    /// <returns>The current status of the permission.</returns>
    Task<PermissionStatus> CheckStatusAsync();

    /// <summary>
    /// Ensures that a required entry matching this permission is found in the application manifest file.
    /// </summary>
    void EnsureDeclared();

    /// <summary>
    /// Requests this permission from the user for this application.
    /// </summary>
    /// <returns>The status of the permission after the request.</returns>
    Task<PermissionStatus> RequestAsync();

    /// <summary>
    /// Determines if an educational UI should be displayed explaining to the user how this permission will be used in the application.
    /// </summary>
    /// <returns>True if an educational UI should be displayed; otherwise, false.</returns>
    bool ShouldShowRationale();
}
