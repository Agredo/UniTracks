using System.Reflection;
using System.Text.Json;
using UniTracks.Models.Trip;

namespace UniTracks.Data.Seeding;

/// <summary>
/// Loads the embedded <c>Data/triptypes.json</c> seed catalog. This is shared by the EF Core
/// <see cref="UniTracks.Data.SQLite.SqliteDBContext"/> (via HasData) and by the cross-platform
/// <see cref="DatabaseInitializer"/>, so the catalog is available on every platform — including
/// iOS, where the store is LiteDB and EF Core's HasData/migration seeding is skipped.
/// </summary>
public static class TripTypeSeeds
{
    public static List<TripType> Load()
    {
        var assembly = typeof(TripTypeSeeds).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("triptypes.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Embedded resource 'triptypes.json' not found in assembly {assembly.FullName}.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        // Parse with JsonDocument rather than reflection-based JsonSerializer.Deserialize: on iOS
        // (IsDynamicCodeSupported=false) reflection-based serialization is not available, so a
        // source-friendly manual read keeps the seed working under AOT / ReadyToRun.
        using var document = JsonDocument.Parse(reader.ReadToEnd());
        var result = new List<TripType>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            result.Add(new TripType
            {
                ID = element.GetProperty("ID").GetGuid(),
                Name = element.GetProperty("Name").GetString() ?? string.Empty,
                Identifier = element.GetProperty("Identifier").GetString() ?? string.Empty,
                Description = element.GetProperty("Description").GetString() ?? string.Empty,
                Category = element.GetProperty("Category").GetString() ?? string.Empty,
            });
        }

        return result;
    }
}
