using UniTracks.Services.ApplicationModel;

using PermissionStatus = UniTracks.Services.ApplicationModel.PermissionStatus;
using MauiPermissionStatus = Microsoft.Maui.ApplicationModel.PermissionStatus;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;


using UniTracks.Services.ApplicationModel.Permissions;

namespace UniTracks.Maui.Services.ApplicationModel;

public class Permissons : IPermissions
{
    public async Task<PermissionStatus> CheckPermissionStatusAsync(Permission permission)
    {
        return MapToPermissionStatus(await CheckMauiPermission(permission));
    }

    public async Task<PermissionStatus> RequestPermissionAsync(Permission permission)
    {
        return MapToPermissionStatus(await RequestMauiPermission(permission));
    }

    public bool ShouldShowRationale(Permission permission)
    {
        return ShowRationalMauiPermission(permission);
    }

    private PermissionStatus MapToPermissionStatus(MauiPermissionStatus mauiPermissionStatus)
    {
        switch (mauiPermissionStatus)
        {
            case MauiPermissionStatus.Limited: return PermissionStatus.Limited;
            case MauiPermissionStatus.Restricted: return PermissionStatus.Restricted;
            case MauiPermissionStatus.Granted: return PermissionStatus.Granted;
            case MauiPermissionStatus.Denied: return PermissionStatus.Denied;
            case MauiPermissionStatus.Unknown: return PermissionStatus.Unknown;
            default: return PermissionStatus.Unknown;
        }
    }

    private async Task<MauiPermissionStatus> CheckMauiPermission(Permission permission)
    {
        switch (permission)
        {
            case Permission.Battery: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Battery>();
            case Permission.Bluetooth: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Bluetooth>();
            case Permission.CalendarRead: return await MauiPermissions.CheckStatusAsync<MauiPermissions.CalendarRead>();
            case Permission.CalendarWrite: return await MauiPermissions.CheckStatusAsync<MauiPermissions.CalendarWrite>();
            case Permission.Camera: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Camera>();
            case Permission.ContactsRead: return await MauiPermissions.CheckStatusAsync<MauiPermissions.ContactsRead>();
            case Permission.ContactsWrite: return await MauiPermissions.CheckStatusAsync<MauiPermissions.ContactsWrite>();
            case Permission.Flashlight: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Flashlight>();
            case Permission.LocationWhenInUse: return await MauiPermissions.CheckStatusAsync<MauiPermissions.LocationWhenInUse>();
            case Permission.LocationAlways: return await MauiPermissions.CheckStatusAsync<MauiPermissions.LocationAlways>();
            case Permission.Media: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Media>();
            case Permission.Microphone: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Microphone>();
            case Permission.NearbyWifiDevices: return await MauiPermissions.CheckStatusAsync<MauiPermissions.NearbyWifiDevices>();
            case Permission.NetworkState: return await MauiPermissions.CheckStatusAsync<MauiPermissions.NetworkState>();
            case Permission.Phone: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Phone>();
            case Permission.Photos: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Photos>();
            case Permission.PhotosAddOnly: return await MauiPermissions.CheckStatusAsync<MauiPermissions.PhotosAddOnly>();
            case Permission.Reminders: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Reminders>();
            case Permission.Sensors: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Sensors>();
            case Permission.Sms: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Sms>();
            case Permission.Speech: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Speech>();
            case Permission.StorageRead: return await MauiPermissions.CheckStatusAsync<MauiPermissions.StorageRead>();
            case Permission.StorageWrite: return await MauiPermissions.CheckStatusAsync<MauiPermissions.StorageWrite>();
            case Permission.Vibrate: return await MauiPermissions.CheckStatusAsync<MauiPermissions.Vibrate>();
            default: return MauiPermissionStatus.Unknown;
        }
    }

    private async Task<MauiPermissionStatus> RequestMauiPermission(Permission permission)
    {
        switch (permission)
        {
            case Permission.Battery: return await MauiPermissions.RequestAsync<MauiPermissions.Battery>();
            case Permission.Bluetooth: return await MauiPermissions.RequestAsync<MauiPermissions.Bluetooth>();
            case Permission.CalendarRead: return await MauiPermissions.RequestAsync<MauiPermissions.CalendarRead>();
            case Permission.CalendarWrite: return await MauiPermissions.RequestAsync<MauiPermissions.CalendarWrite>();
            case Permission.Camera: return await MauiPermissions.RequestAsync<MauiPermissions.Camera>();
            case Permission.ContactsRead: return await MauiPermissions.RequestAsync<MauiPermissions.ContactsRead>();
            case Permission.ContactsWrite: return await MauiPermissions.RequestAsync<MauiPermissions.ContactsWrite>();
            case Permission.Flashlight: return await MauiPermissions.RequestAsync<MauiPermissions.Flashlight>();
            case Permission.LocationWhenInUse: return await MauiPermissions.RequestAsync<MauiPermissions.LocationWhenInUse>();
            case Permission.LocationAlways: return await MauiPermissions.RequestAsync<MauiPermissions.LocationAlways>();
            case Permission.Media: return await MauiPermissions.RequestAsync<MauiPermissions.Media>();
            case Permission.Microphone: return await MauiPermissions.RequestAsync<MauiPermissions.Microphone>();
            case Permission.NearbyWifiDevices: return await MauiPermissions.RequestAsync<MauiPermissions.NearbyWifiDevices>();
            case Permission.NetworkState: return await MauiPermissions.RequestAsync<MauiPermissions.NetworkState>();
            case Permission.Phone: return await MauiPermissions.RequestAsync<MauiPermissions.Phone>();
            case Permission.Photos: return await MauiPermissions.RequestAsync<MauiPermissions.Photos>();
            case Permission.PhotosAddOnly: return await MauiPermissions.RequestAsync<MauiPermissions.PhotosAddOnly>();
            case Permission.Reminders: return await MauiPermissions.RequestAsync<MauiPermissions.Reminders>();
            case Permission.Sensors: return await MauiPermissions.RequestAsync<MauiPermissions.Sensors>();
            case Permission.Sms: return await MauiPermissions.RequestAsync<MauiPermissions.Sms>();
            case Permission.Speech: return await MauiPermissions.RequestAsync<MauiPermissions.Speech>();
            case Permission.StorageRead: return await MauiPermissions.RequestAsync<MauiPermissions.StorageRead>();
            case Permission.StorageWrite: return await MauiPermissions.RequestAsync<MauiPermissions.StorageWrite>();
            case Permission.Vibrate: return await MauiPermissions.RequestAsync<MauiPermissions.Vibrate>();
            default: return MauiPermissionStatus.Unknown;
        }
    }

    private bool ShowRationalMauiPermission(Permission permission)
    {
        switch (permission)
        {
            case Permission.Battery: return MauiPermissions.ShouldShowRationale<MauiPermissions.Battery>();
            case Permission.Bluetooth: return MauiPermissions.ShouldShowRationale<MauiPermissions.Bluetooth>();
            case Permission.CalendarRead: return MauiPermissions.ShouldShowRationale<MauiPermissions.CalendarRead>();
            case Permission.CalendarWrite: return MauiPermissions.ShouldShowRationale<MauiPermissions.CalendarWrite>();
            case Permission.Camera: return MauiPermissions.ShouldShowRationale<MauiPermissions.Camera>();
            case Permission.ContactsRead: return MauiPermissions.ShouldShowRationale<MauiPermissions.ContactsRead>();
            case Permission.ContactsWrite: return MauiPermissions.ShouldShowRationale<MauiPermissions.ContactsWrite>();
            case Permission.Flashlight: return MauiPermissions.ShouldShowRationale<MauiPermissions.Flashlight>();
            case Permission.LocationWhenInUse: return MauiPermissions.ShouldShowRationale<MauiPermissions.LocationWhenInUse>();
            case Permission.LocationAlways: return MauiPermissions.ShouldShowRationale<MauiPermissions.LocationAlways>();
            case Permission.Media: return MauiPermissions.ShouldShowRationale<MauiPermissions.Media>();
            case Permission.Microphone: return MauiPermissions.ShouldShowRationale<MauiPermissions.Microphone>();
            case Permission.NearbyWifiDevices: return MauiPermissions.ShouldShowRationale<MauiPermissions.NearbyWifiDevices>();
            case Permission.NetworkState: return MauiPermissions.ShouldShowRationale<MauiPermissions.NetworkState>();
            case Permission.Phone: return MauiPermissions.ShouldShowRationale<MauiPermissions.Phone>();
            case Permission.Photos: return MauiPermissions.ShouldShowRationale<MauiPermissions.Photos>();
            case Permission.PhotosAddOnly: return MauiPermissions.ShouldShowRationale<MauiPermissions.PhotosAddOnly>();
            case Permission.Reminders: return MauiPermissions.ShouldShowRationale<MauiPermissions.Reminders>();
            case Permission.Sensors: return MauiPermissions.ShouldShowRationale<MauiPermissions.Sensors>();
            case Permission.Sms: return MauiPermissions.ShouldShowRationale<MauiPermissions.Sms>();
            case Permission.Speech: return MauiPermissions.ShouldShowRationale<MauiPermissions.Speech>();
            case Permission.StorageRead: return MauiPermissions.ShouldShowRationale<MauiPermissions.StorageRead>();
            case Permission.StorageWrite: return MauiPermissions.ShouldShowRationale<MauiPermissions.StorageWrite>();
            case Permission.Vibrate: return MauiPermissions.ShouldShowRationale<MauiPermissions.Vibrate>();
            default: return false;
        }
    }
}
