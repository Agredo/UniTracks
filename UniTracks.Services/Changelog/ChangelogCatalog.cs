using System.Text.Json;

namespace UniTracks.Services.Changelog;

/// <summary>
/// Parses a changelog document into <see cref="ChangelogRelease"/> items.
/// </summary>
/// <remarks>
/// <see cref="JsonDocument"/> is used rather than reflection-based
/// <c>JsonSerializer.Deserialize</c> because iOS runs AOT / ReadyToRun with
/// <c>IsDynamicCodeSupported=false</c>, where reflection-based serialization is unavailable.
/// </remarks>
public static class ChangelogCatalog
{
    /// <summary>
    /// Reads a changelog document. Both <c>{ "releases": [ … ] }</c> and a bare <c>[ … ]</c> array are
    /// accepted, and a <c>changes</c> entry may be a plain string or an object carrying
    /// <c>kind</c> and <c>text</c>. Unusable input yields an empty list instead of throwing, so a
    /// malformed file can never take the app down. The result is sorted newest first.
    /// </summary>
    public static IReadOnlyList<ChangelogRelease> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return [];
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("releases", out var nested) &&
                nested.ValueKind == JsonValueKind.Array)
            {
                root = nested;
            }

            if (root.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var parsed = new List<ChangelogRelease>();
            foreach (var element in root.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var version = ReadString(element, "version");
                if (string.IsNullOrWhiteSpace(version))
                {
                    continue;
                }

                parsed.Add(new ChangelogRelease
                {
                    Version = version,
                    Date = ReadString(element, "date"),
                    Title = ReadString(element, "title"),
                    Changes = ReadChanges(element),
                });
            }

            parsed.Sort((left, right) => ChangelogVersion.Compare(right.Version, left.Version));
            return parsed;
        }
    }

    private static IReadOnlyList<ChangelogChange> ReadChanges(JsonElement release)
    {
        if (!release.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<ChangelogChange>();
        foreach (var element in changes.EnumerateArray())
        {
            var text = element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Object => ReadString(element, "text"),
                _ => null,
            };

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            parsed.Add(new ChangelogChange
            {
                Text = text,
                Kind = element.ValueKind == JsonValueKind.Object
                    ? ParseKind(ReadString(element, "kind"))
                    : ChangelogChangeKind.Improvement,
            });
        }

        return parsed;
    }

    private static ChangelogChangeKind ParseKind(string kind)
        => kind.Trim().ToLowerInvariant() switch
        {
            "feature" or "neu" or "new" => ChangelogChangeKind.Feature,
            "fix" or "bugfix" or "behoben" => ChangelogChangeKind.Fix,
            _ => ChangelogChangeKind.Improvement,
        };

    private static string ReadString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
