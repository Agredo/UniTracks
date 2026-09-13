using System.Diagnostics;

namespace UniTracks.Maui.Services.ApplicationModel;

/// <summary>
/// Opens the platform settings page for this app (<c>AppInfo.ShowSettingsUI()</c>). Used for the
/// Android background location permission, which Android 11+ only grants from the system page.
/// </summary>
public class AppSettings : UniTracks.Services.ApplicationModel.IAppSettings
{
    public Task<bool> ShowAsync()
    {
        try
        {
            Microsoft.Maui.ApplicationModel.AppInfo.Current.ShowSettingsUI();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            // Nothing the user can do about a settings page that does not open; never let it bubble
            // up into the recording flow.
            Debug.WriteLine($"[UniTracks] Could not show the app settings: {ex}");
            return Task.FromResult(false);
        }
    }
}
