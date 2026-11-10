using UniTracks.Data.Importing;
using UniTracks.Data.Seeding;
using UniTracks.Data.SQLite;
using UniTracks.Games.CityBuilder.Persistence;
using UniTracks.Models.Comparison;
using UniTracks.Models.Environment;
using UniTracks.Models.Health;
using LocationModel = UniTracks.Models.Location.Location;
using UniTracks.Models.Trip;
using UniTracks.Models.User;
using LiteDbStore = UniTracks.Data.LiteDB.LiteDatabase;

namespace UniTracks.Tests.Data;

/// <summary>
/// The iOS store keeps a trip's GPS points, heart rates, weights and weather inside the trip
/// document, while SQLite uses tables with (partly shadow) foreign keys. These tests pin the
/// rewrite that turns one layout into the other.
/// </summary>
public sealed class LiteDatabaseImporterTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "UniTracks-Import-" + Guid.NewGuid().ToString("N"));

    public LiteDatabaseImporterTests() => Directory.CreateDirectory(directory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception)
        {
            // A leftover temp directory is not worth failing a test for.
        }
    }

    [Fact]
    public void Import_FlattensTheTripGraphIntoTheRelationalTables()
    {
        var tripType = TripTypeSeeds.Load()[0];
        var user = new User { ID = Guid.NewGuid(), Name = "Test" };
        var points = CreatePoints(3);
        var heartRate = new HeartRate { ID = Guid.NewGuid(), Rate = 120, Timestamp = DateTimeOffset.Now };
        var weight = new Weight { ID = Guid.NewGuid(), WeightValue = 78.5, Timestamp = DateTimeOffset.Now, UserID = user.ID };
        var weather = new Weather
        {
            ID = Guid.NewGuid(),
            Temperature = 19,
            Timestamp = DateTimeOffset.Now,
            Location = points[0],
        };
        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            Name = "Feierabendrunde",
            StartTime = DateTimeOffset.Now.AddHours(-1),
            EndTime = DateTimeOffset.Now,
            TripTypeId = tripType.ID,
            TripType = tripType,
            Locations = points,
            HeartRates = [heartRate],
            Weights = [weight],
            Weather = [weather],
        };
        var fingerprint = new TripFingerprint { TripID = trip.ID, PointCount = 3, DistanceMeters = 4200 };

        using (var source = new LiteDbStore(SourcePath()))
        {
            source.Database.GetCollection<Trip>().Insert(trip);
            source.Database.GetCollection<User>().Insert(user);
            source.Database.GetCollection<TripFingerprint>().Insert(fingerprint);
            source.Database.GetCollection<TripType>().Insert(tripType);
            source.Database.GetCollection<PlacedBuilding>().Insert(new PlacedBuilding { ID = Guid.NewGuid() });
        }

        var seededTripTypes = TripTypeSeeds.Load().Count;
        var summary = LiteDatabaseImporter.ImportIntoSqlite(SourcePath(), TargetPath());

        Assert.Equal(1, summary.Trips);
        Assert.Equal(3, summary.Locations);
        Assert.Equal(1, summary.Fingerprints);
        Assert.Equal(1, summary.Users);
        Assert.Equal(1, summary.GameRows);

        using var target = new SqliteDBContext(TargetPath());

        // The catalogue was already seeded, so the imported copy must not show up twice.
        Assert.Equal(seededTripTypes, target.TripTypes.Count());

        var storedTrip = Assert.Single(target.Trips);
        Assert.Equal(trip.ID, storedTrip.ID);
        Assert.Equal(tripType.ID, storedTrip.TripTypeId);

        Assert.Equal(3, target.Locations.Count());
        Assert.All(target.Locations, location => Assert.Equal(trip.ID, location.TripID));

        var storedHeartRate = Assert.Single(target.HeartRates);
        Assert.Equal(trip.ID, target.Entry(storedHeartRate).Property<Guid?>("TripID").CurrentValue);

        var storedWeight = Assert.Single(target.Weights);
        Assert.Equal(trip.ID, target.Entry(storedWeight).Property<Guid?>("TripID").CurrentValue);
        Assert.Equal(user.ID, storedWeight.UserID);

        var storedWeather = Assert.Single(target.Weathers);
        Assert.Equal(trip.ID, target.Entry(storedWeather).Property<Guid?>("TripID").CurrentValue);
        Assert.Equal(points[0].ID, target.Entry(storedWeather).Property<Guid>("LocationID").CurrentValue);

        Assert.Equal(trip.ID, Assert.Single(target.TripFingerprints).TripID);
        Assert.Equal(user.ID, Assert.Single(target.Users).ID);
        Assert.Single(target.PlacedBuildings);
    }

    [Fact]
    public void Import_MergesTheStandaloneLocationRowsWithTheEmbeddedOnes()
    {
        // The recording writes every GPS point twice: into the trip document and into its own
        // collection. Importing both as they are would either duplicate the track or lose the
        // trip link, depending on which copy is read first.
        var trip = new Trip { ID = Guid.NewGuid(), Locations = CreatePoints(2) };
        var standalone = new LocationModel
        {
            ID = trip.Locations[0].ID,
            Latitude = 52.5,
            Longitude = 13.4,
            Altitude = 34,
            TripID = trip.ID,
        };

        using (var source = new LiteDbStore(SourcePath()))
        {
            source.Database.GetCollection<Trip>().Insert(trip);
            source.Database.GetCollection<LocationModel>().Insert(standalone);
        }

        var summary = LiteDatabaseImporter.ImportIntoSqlite(SourcePath(), TargetPath());

        Assert.Equal(2, summary.Locations);

        using var target = new SqliteDBContext(TargetPath());
        Assert.Equal(2, target.Locations.Count());
        Assert.Equal(52.5, target.Locations.Single(row => row.ID == standalone.ID).Latitude);
        Assert.All(target.Locations, location => Assert.Equal(trip.ID, location.TripID));
    }

    [Fact]
    public void Import_DropsFingerprintsWhoseTripIsMissing()
    {
        var known = Guid.NewGuid();
        var trip = new Trip { ID = known, Locations = CreatePoints(2) };

        using (var source = new LiteDbStore(SourcePath()))
        {
            var fingerprints = source.Database.GetCollection<TripFingerprint>();
            fingerprints.Insert(new TripFingerprint { TripID = known, Version = 1 });
            fingerprints.Insert(new TripFingerprint { TripID = Guid.NewGuid(), Version = 1 });
            source.Database.GetCollection<Trip>().Insert(trip);
        }

        var summary = LiteDatabaseImporter.ImportIntoSqlite(SourcePath(), TargetPath());

        // A fingerprint without its trip would break the foreign key.
        Assert.Equal(1, summary.Fingerprints);

        using var target = new SqliteDBContext(TargetPath());
        Assert.Equal(known, Assert.Single(target.TripFingerprints).TripID);
    }

    [Fact]
    public void Import_DetachesLocationsWhoseTripIsNotPartOfTheBackup()
    {
        var orphan = new LocationModel { ID = Guid.NewGuid(), Latitude = 48, Longitude = 11, TripID = Guid.NewGuid() };

        using (var source = new LiteDbStore(SourcePath()))
        {
            source.Database.GetCollection<LocationModel>().Insert(orphan);
        }

        LiteDatabaseImporter.ImportIntoSqlite(SourcePath(), TargetPath());

        using var target = new SqliteDBContext(TargetPath());
        Assert.Null(Assert.Single(target.Locations).TripID);
    }

    private string SourcePath() => Path.Combine(directory, "ios-litedb.db");

    private string TargetPath() => Path.Combine(directory, "converted-sqlite.db");

    private static List<LocationModel> CreatePoints(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new LocationModel
            {
                ID = Guid.NewGuid(),
                Latitude = 52.5 + index * 0.001,
                Longitude = 13.4 + index * 0.001,
                Altitude = 34 + index,
                Timestamp = DateTimeOffset.Now.AddMinutes(-index),
            })
            .ToList();
}
