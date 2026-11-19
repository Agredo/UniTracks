using UniTracks.Services.Settings;

namespace UniTracks.Maui.Services.Settings;

/// <inheritdoc />
public sealed class PreferencesRecordingSettings : IRecordingSettings
{
    private const string LastTripTypeIdKey = "UniTracks.Settings.Recording.LastTripTypeId";
    private const string RecentTripTypeIdsKey = "UniTracks.Settings.Recording.RecentTripTypeIds";

    public string LastTripTypeId
    {
        get => Preferences.Default.Get(LastTripTypeIdKey, string.Empty);
        set => Preferences.Default.Set(LastTripTypeIdKey, value);
    }

    /// <summary>Stored as a comma-separated GUID list, newest entry first.</summary>
    public IReadOnlyList<Guid> RecentTripTypeIds
    {
        get => Parse(Preferences.Default.Get(RecentTripTypeIdsKey, string.Empty));
        set => Preferences.Default.Set(
            RecentTripTypeIdsKey,
            string.Join(',', value.Select(id => id.ToString())));
    }

    /// <summary>
    /// Tolerates a malformed or partially written value: unknown entries are dropped instead of
    /// throwing, because a broken preference must not keep the record page from opening.
    /// </summary>
    private static IReadOnlyList<Guid> Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<Guid>();
        }

        var ids = new List<Guid>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
