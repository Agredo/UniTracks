namespace UniTracks.Services.ApplicationModel;

/// <summary>
/// Opens the operating system's settings page for this app. Needed for permissions the platform only
/// grants from there: Android 11+ no longer offers "Allow all the time" in the runtime location
/// dialog, so the user has to be taken to the system page.
/// </summary>
public interface IAppSettings
{
    /// <summary>Opens the app's settings page. Returns false when it could not be shown.</summary>
    Task<bool> ShowAsync();
}
