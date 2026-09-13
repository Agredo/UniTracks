namespace UniTracks.Services.Settings;

/// <summary>
/// User-facing switch for the trip list card density. <c>true</c>: compact one-line cards that
/// skip the stats grid and the extras row (weather, heart rate, elevation) so more trips fit on
/// screen. <c>false</c> (default): the full card layout.
/// </summary>
public interface ITripCardLayoutSettings
{
    bool IsCompact { get; set; }
}
