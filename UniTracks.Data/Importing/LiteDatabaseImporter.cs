using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Reflection;
using UniTracks.Data.SQLite;
using UniTracks.Games.BaseCamp.Persistence;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Models.Comparison;
using UniTracks.Models.Environment;
using UniTracks.Models.Health;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Models.User;
using LiteDbStore = UniTracks.Data.LiteDB.LiteDatabase;

namespace UniTracks.Data.Importing;

/// <summary>What a converted iOS backup brought into the SQLite store.</summary>
public sealed record LiteDatabaseImportSummary(
    int Trips,
    int Locations,
    int Fingerprints,
    int Users,
    int GameRows);

/// <summary>
/// Copies the contents of an iOS database (LiteDB) into a fresh SQLite store, so a backup taken on
/// iOS can be imported on Android, Mac Catalyst and Windows.
/// <para>
/// The two stores lay the same models out differently: LiteDB keeps a trip's GPS points, heart rates,
/// weights and weather as embedded documents, while SQLite stores them in their own tables with
/// foreign keys (some of them shadow properties, e.g. <c>HeartRate.TripID</c>). This class flattens
/// the document graph into those tables and wires the keys up again — which is why copying the file
/// alone cannot work.
/// </para>
/// </summary>
public static class LiteDatabaseImporter
{
    private static readonly ConcurrentDictionary<Type, Lazy<PropertyInfo[]>> RequiredMembers = new();

    /// <summary>
    /// Imports every collection of <paramref name="liteDatabasePath"/> into a new database at
    /// <paramref name="sqliteDatabasePath"/>. The target must not exist yet: it is created with the
    /// current schema and the shipped trip-type catalog. Throws when the source cannot be read.
    /// </summary>
    public static LiteDatabaseImportSummary ImportIntoSqlite(string liteDatabasePath, string sqliteDatabasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(liteDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteDatabasePath);

        // Opening through the app's own wrapper installs the key mapping (Guid keys on "_id") and
        // repairs documents written before that mapping existed, so any app version can be read.
        using var source = new LiteDbStore(liteDatabasePath);

        // Creates the schema (migrations) and seeds the trip-type catalog.
        using var target = new SqliteDBContext(sqliteDatabasePath);

        var trips = ReadAll<Trip>(source);
        var users = ReadAll<User>(source);
        var tripTypes = ReadAll<TripType>(source);
        var fingerprints = ReadAll<TripFingerprint>(source);

        // On iOS the children live inside the trip document; the recording also writes the GPS
        // points to their own collection. Both sources are merged by key, so a track is never lost
        // and no row is inserted twice.
        var locations = new Dictionary<Guid, Location>();
        var heartRates = new Dictionary<Guid, HeartRate>();
        var weights = new Dictionary<Guid, Weight>();
        var weather = new Dictionary<Guid, Weather>();
        var tripOfHeartRate = new Dictionary<Guid, Guid>();
        var tripOfWeight = new Dictionary<Guid, Guid>();
        var tripOfWeather = new Dictionary<Guid, Guid>();
        var locationOfWeather = new Dictionary<Guid, Guid>();

        foreach (var trip in trips)
        {
            foreach (var location in trip.Locations ?? [])
            {
                location.TripID ??= trip.ID;
                locations[location.ID] = location;
            }

            foreach (var heartRate in trip.HeartRates ?? [])
            {
                heartRates[heartRate.ID] = heartRate;
                tripOfHeartRate[heartRate.ID] = trip.ID;
            }

            foreach (var weight in trip.Weights ?? [])
            {
                weights[weight.ID] = weight;
                tripOfWeight[weight.ID] = trip.ID;
            }

            foreach (var entry in trip.Weather ?? [])
            {
                weather[entry.ID] = entry;
                tripOfWeather[entry.ID] = trip.ID;

                if (entry.Location is { } where)
                {
                    locationOfWeather[entry.ID] = where.ID;
                }
            }
        }

        // Rows the app wrote straight into their own collection carry the complete data (including
        // the trip link, which the embedded copy may lack), so they win over the embedded copy.
        foreach (var location in ReadAll<Location>(source))
        {
            // Should the standalone row have lost its trip link, the embedded copy still has it.
            if (locations.TryGetValue(location.ID, out var embedded))
            {
                location.TripID ??= embedded.TripID;
            }

            locations[location.ID] = location;
        }

        foreach (var weight in ReadAll<Weight>(source))
        {
            // An embedded weight keeps its trip link, so it is not replaced by the standalone row.
            if (!weights.ContainsKey(weight.ID))
            {
                weights[weight.ID] = weight;
            }
        }

        // A weather row needs its location row to exist; embedded weather locations are only
        // stored inside the trip document.
        foreach (var entry in weather.Values)
        {
            if (entry.Location is { } where)
            {
                locations.TryAdd(where.ID, where);
            }
        }

        var tripIDs = trips.Select(trip => trip.ID).ToHashSet();

        // A point whose trip is not part of the import would break the foreign key.
        foreach (var location in locations.Values.Where(row => row.TripID is { } owner && !tripIDs.Contains(owner)))
        {
            location.TripID = null;
        }

        foreach (var trip in trips)
        {
            // The foreign key (TripTypeId) carries the link; the navigation would be inserted as a
            // second copy of an already existing trip type.
            trip.TripType = null;
            trip.Locations = [];
            trip.HeartRates = null;
            trip.Weights = null;
            trip.Weather = null;
        }

        foreach (var user in users)
        {
            user.Trips = null;
            user.Weights = null;
        }

        var seededTripTypes = target.TripTypes.Select(tripType => tripType.ID).ToHashSet();
        var addedTripTypes = new HashSet<Guid>();

        foreach (var tripType in tripTypes)
        {
            // The migration already seeded the shipped catalog, and its rows are the current ones.
            if (seededTripTypes.Contains(tripType.ID) || !addedTripTypes.Add(tripType.ID))
            {
                continue;
            }

            AddRows(target, [tripType]);
        }

        AddRows(target, users);
        AddRows(target, trips);
        AddRows(target, locations.Values);
        AddRows(target, heartRates.Values);
        AddRows(target, weights.Values);

        // Weather rows without a usable location cannot be stored (the relation is required).
        var importedWeather = weather.Values
            .Where(entry => !locationOfWeather.TryGetValue(entry.ID, out var where) || locations.ContainsKey(where))
            .ToList();

        // The weather's embedded location is a separate instance of the row that the location table
        // already holds, so attaching both graphs would give EF two instances for one key. The link
        // is rebuilt from the keys below instead.
        foreach (var entry in importedWeather)
        {
            entry.Location = null!;
        }

        AddRows(target, importedWeather);

        // TripID only exists as a shadow property on these three tables, so the owner has to be
        // written through the change tracker instead of a navigation.
        foreach (var heartRate in heartRates.Values)
        {
            SetKey(target, heartRate, "TripID", tripOfHeartRate.GetValueOrDefault(heartRate.ID));
        }

        foreach (var weight in weights.Values)
        {
            SetKey(target, weight, "TripID", tripOfWeight.GetValueOrDefault(weight.ID));
        }

        foreach (var entry in importedWeather)
        {
            SetKey(target, entry, "TripID", tripOfWeather.GetValueOrDefault(entry.ID));

            if (locationOfWeather.TryGetValue(entry.ID, out var where))
            {
                SetKey(target, entry, "LocationID", where);
            }
        }

        var importedFingerprints = fingerprints
            .Where(fingerprint => tripIDs.Contains(fingerprint.TripID))
            .DistinctBy(fingerprint => fingerprint.TripID)
            .ToList();

        AddRows(target, importedFingerprints);

        // The games keep flat rows on both stores, so they are copied one to one.
        var gameRows = AddAll<PlacedBuilding>(source, target)
            + AddAll<CityExpansion>(source, target)
            + AddAll<TowerUnlock>(source, target)
            + AddAll<EnergyPurchase>(source, target)
            + AddAll<DefenseRecord>(source, target)
            + AddAll<DefenseRunProgress>(source, target)
            + AddAll<CampModule>(source, target)
            + AddAll<CampLog>(source, target);

        target.SaveChanges();

        // EF Core pools SQLite connections, and a pooled handle keeps the freshly written file
        // locked. The caller moves that file into place right after this call, so the handle has to
        // be gone first.
        target.Database.CloseConnection();
        SqliteConnection.ClearAllPools();

        return new LiteDatabaseImportSummary(
            trips.Count,
            locations.Count,
            importedFingerprints.Count,
            users.Count,
            gameRows);
    }

    private static List<TEntity> ReadAll<TEntity>(LiteDbStore source) where TEntity : class =>
        source.Database.GetCollection<TEntity>().FindAll().ToList();

    private static void AddRows<TEntity>(DbContext target, IEnumerable<TEntity> rows) where TEntity : class
    {
        var prepared = DistinctByID(rows).Select(Normalize).ToList();

        if (prepared.Count > 0)
        {
            target.AddRange(prepared);
        }
    }

    private static int AddAll<TEntity>(LiteDbStore source, DbContext target) where TEntity : class
    {
        var rows = DistinctByID(ReadAll<TEntity>(source)).Select(Normalize).ToList();

        if (rows.Count > 0)
        {
            target.AddRange(rows);
        }

        return rows.Count;
    }

    /// <summary>
    /// Restores the declared default of required strings, arrays and lists. LiteDB only stores members
    /// that differ from their default, so such a member comes back as null even though the model
    /// declares it non-nullable — and SQLite rejects the null.
    /// </summary>
    private static TEntity Normalize<TEntity>(TEntity entity) where TEntity : class
    {
        var members = RequiredMembers
            .GetOrAdd(entity.GetType(), type => new Lazy<PropertyInfo[]>(() => FindRequiredMembers(type)))
            .Value;

        foreach (var property in members)
        {
            if (property.GetValue(entity) is not null)
            {
                continue;
            }

            property.SetValue(entity, property.PropertyType == typeof(string)
                ? string.Empty
                : Activator.CreateInstance(property.PropertyType));
        }

        return entity;
    }

    private static PropertyInfo[] FindRequiredMembers(Type type)
    {
        // NullabilityInfoContext is not thread-safe, so every call gets its own instance.
        var nullability = new NullabilityInfoContext();

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite && IsFillable(property.PropertyType))
            .Where(property => nullability.Create(property).ReadState == NullabilityState.NotNull)
            .ToArray();
    }

    private static bool IsFillable(Type type) =>
        type == typeof(string)
        || type.IsArray
        || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));

    /// <summary>
    /// Removes duplicate rows by primary key. LiteDB does not enforce a key, so a document written
    /// before the key mapping existed can exist twice; SQLite would reject the second one.
    /// </summary>
    private static IEnumerable<TEntity> DistinctByID<TEntity>(IEnumerable<TEntity> rows) where TEntity : class =>
        rows.DistinctBy(row => KeyOf(row));

    private static Guid KeyOf<TEntity>(TEntity entity) where TEntity : class =>
        entity.GetType().GetProperty("ID")?.GetValue(entity) is Guid id ? id : Guid.Empty;

    private static void SetKey<TEntity>(DbContext target, TEntity entity, string propertyName, Guid value) where TEntity : class
    {
        if (value == Guid.Empty)
        {
            return;
        }

        target.Entry(entity).Property(propertyName).CurrentValue = value;
    }
}
