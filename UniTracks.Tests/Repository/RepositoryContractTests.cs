using BsonDocument = LiteDB.BsonDocument;
using ObjectId = LiteDB.ObjectId;
using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Models.Comparison;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;
using UniTracks.Tests.TestSupport;
// The model type lives in the namespace UniTracks.Models.Location, which would otherwise be
// shadowed by the sibling test namespace UniTracks.Tests.Location.
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.Repository;

/// <summary>
/// Provider-agnostic contract tests for <see cref="IRepository"/>. Every assertion runs against both
/// implementations, because the UI layer only ever sees the abstraction and the two stores must
/// behave the same: EF Core + SQLite on Android/Mac Catalyst/Windows, LiteDB on iOS.
/// <para>
/// Covers finding #7: <c>DeleteRange</c> exists so a batch of child rows (e.g. a trip's locations)
/// can be removed in one operation instead of leaving orphans behind.
/// </para>
/// </summary>
public sealed class RepositoryContractTests
{
    private static TowerUnlock Unlock(int index) => new()
    {
        ID = Guid.NewGuid(),
        TowerId = $"tower-{index}",
    };

    [Fact]
    public async Task Ef_AddThenRead_ReturnsTheStoredEntity()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;
        var entity = Unlock(1);

        await repository.Add(entity);

        var byId = await repository.GetByIdAsync<TowerUnlock>(entity.ID);
        Assert.NotNull(byId);
        Assert.Equal(entity.TowerId, byId!.TowerId);
        Assert.Equal(entity.PurchasedAt, byId.PurchasedAt);

        Assert.Single(repository.Get<TowerUnlock>());
        Assert.Single(repository.Get<TowerUnlock>(u => u.TowerId == "tower-1"));
        Assert.Empty(repository.Get<TowerUnlock>(u => u.TowerId == "tower-2"));
        Assert.Single(await repository.GetAllAsync<TowerUnlock>());
    }

    [Fact]
    public async Task Ef_Update_PersistsScalarChanges()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            Name = "Feierabendrunde",
            Locations = new List<LocationModel>(),
        };
        await repository.Add(trip);

        trip.Name = "Feierabendrunde (korrigiert)";
        trip.Distance = 1234.5;
        await repository.Update(trip);

        var stored = Assert.Single(repository.Get<Trip>());
        Assert.Equal("Feierabendrunde (korrigiert)", stored.Name);
        Assert.Equal(1234.5, stored.Distance);
    }

    /// <summary>
    /// Mirrors the exact order used by <c>GpsDataStorageService.StoreData</c>: the new location is
    /// put into the trip's graph, persisted on its own, and only then is the parent trip updated.
    /// <c>Update</c> marks just the root's scalar properties as modified — a full graph write would
    /// UPDATE a row that does not exist yet and raise <c>DbUpdateConcurrencyException</c>
    /// (the Android GPS recording crash fixed in a94fc8d).
    /// </summary>
    [Fact]
    public async Task Ef_Update_AfterChildWasPersistedSeparately_DoesNotThrow()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        var trip = new Trip
        {
            ID = Guid.NewGuid(),
            Name = "Mit Kindern",
            Locations = new List<LocationModel>(),
        };
        await repository.Add(trip);

        var location = new LocationModel
        {
            ID = Guid.NewGuid(),
            Latitude = 48.2,
            Longitude = 11.6,
            TripID = trip.ID,
        };

        trip.Locations.Add(location);
        trip.Distance = 4321.0;
        await repository.Add(location);
        await repository.Update(trip);

        var storedTrip = Assert.Single(repository.Get<Trip>());
        Assert.Equal(4321.0, storedTrip.Distance);

        var storedLocation = Assert.Single(repository.Get<LocationModel>());
        Assert.Equal(location.ID, storedLocation.ID);
        Assert.Equal(trip.ID, storedLocation.TripID);
    }

    [Fact]
    public async Task Ef_Delete_RemovesOnlyThatEntity()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        var first = await repository.Add(Unlock(1));
        var second = await repository.Add(Unlock(2));

        await repository.Delete(first);

        var remaining = Assert.Single(await repository.GetAllAsync<TowerUnlock>());
        Assert.Equal(second.ID, remaining.ID);
        Assert.Null(await repository.GetByIdAsync<TowerUnlock>(first.ID));
    }

    [Fact]
    public async Task Ef_DeleteRange_RemovesExactlyTheGivenEntities()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        var entities = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => repository.Add(Unlock(i))));
        var doomed = entities.Where((_, index) => index % 2 == 0).ToList();
        var survivors = entities.Where((_, index) => index % 2 != 0).ToList();

        await repository.DeleteRange(doomed);

        var remaining = (await repository.GetAllAsync<TowerUnlock>()).ToList();
        Assert.Equal(survivors.Count, remaining.Count);
        Assert.Equal(
            survivors.Select(u => u.ID).OrderBy(id => id),
            remaining.Select(u => u.ID).OrderBy(id => id));
    }

    [Fact]
    public async Task Ef_DeleteRange_IsANoOpForAnEmptySequence()
    {
        using var db = new SqliteTestDatabase();
        var repository = db.Repository;

        await repository.Add(Unlock(1));
        await repository.DeleteRange(Array.Empty<TowerUnlock>());

        Assert.Single(await repository.GetAllAsync<TowerUnlock>());
    }

    [Fact]
    public async Task LiteDb_AddThenRead_ReturnsTheStoredEntity()
    {
        await WithLiteDb(async repository =>
        {
            var entity = Unlock(1);
            await repository.Add(entity);

            var byId = await repository.GetByIdAsync<TowerUnlock>(entity.ID);
            Assert.NotNull(byId);
            Assert.Equal(entity.TowerId, byId!.TowerId);

            Assert.Single(repository.Get<TowerUnlock>());
            Assert.Single(repository.Get<TowerUnlock>(u => u.TowerId == "tower-1"));
            Assert.Empty(repository.Get<TowerUnlock>(u => u.TowerId == "tower-2"));
            Assert.Single(await repository.GetAllAsync<TowerUnlock>());
        });
    }

    [Fact]
    public async Task LiteDb_Delete_RemovesOnlyThatEntity()
    {
        await WithLiteDb(async repository =>
        {
            var first = await repository.Add(Unlock(1));
            var second = await repository.Add(Unlock(2));

            await repository.Delete(first);

            var remaining = Assert.Single(await repository.GetAllAsync<TowerUnlock>());
            Assert.Equal(second.ID, remaining.ID);
        });
    }

    [Fact]
    public async Task LiteDb_DeleteRange_RemovesExactlyTheGivenEntities()
    {
        await WithLiteDb(async repository =>
        {
            var entities = new List<TowerUnlock>();
            for (var i = 0; i < 10; i++)
            {
                entities.Add(await repository.Add(Unlock(i)));
            }

            var doomed = entities.Where((_, index) => index % 2 == 0).ToList();
            var survivors = entities.Where((_, index) => index % 2 != 0).ToList();

            await repository.DeleteRange(doomed);

            var remaining = (await repository.GetAllAsync<TowerUnlock>()).ToList();
            Assert.Equal(survivors.Count, remaining.Count);
            Assert.Equal(
                survivors.Select(u => u.ID).OrderBy(id => id),
                remaining.Select(u => u.ID).OrderBy(id => id));
        });
    }

    [Fact]
    public async Task LiteDb_DeleteRange_IsANoOpForAnEmptySequence()
    {
        await WithLiteDb(async repository =>
        {
            await repository.Add(Unlock(1));
            await repository.DeleteRange(Array.Empty<TowerUnlock>());

            Assert.Single(await repository.GetAllAsync<TowerUnlock>());
        });
    }

    /// <summary>
    /// Whole-table reads are served from a read-through cache (that is what keeps the statistics
    /// page from re-scanning every trip and its thousands of GPS points on each visit). The cache
    /// must never outlive a write: every mutation has to make the next full read hit the store again.
    /// </summary>
    [Fact]
    public async Task Ef_FullReads_SeeEarlierWrites()
    {
        using var db = new SqliteTestDatabase();
        await AssertFullReadsSeeWrites(db.Repository);
    }

    /// <inheritdoc cref="Ef_FullReads_SeeEarlierWrites"/>
    [Fact]
    public async Task LiteDb_FullReads_SeeEarlierWrites()
    {
        await WithLiteDb(AssertFullReadsSeeWrites);
    }

    private static async Task AssertFullReadsSeeWrites(IRepository repository)
    {
        static DefenseRecord Record(int wave) => new()
        {
            ID = Guid.NewGuid(),
            BestWave = wave,
            BestScore = wave * 100,
        };

        // Prime the cache with a plain read, then mutate through every write method.
        var first = await repository.Add(Record(1));
        Assert.Single(await repository.GetAllAsync<DefenseRecord>());
        Assert.Single(repository.Get<DefenseRecord>());
        Assert.Single(await repository.GetAsync<DefenseRecord>());

        var second = await repository.Add(Record(2));
        Assert.Equal(2, (await repository.GetAllAsync<DefenseRecord>()).Count());
        Assert.Equal(2, repository.Get<DefenseRecord>().Count());

        first.BestWave = 7;
        await repository.Update(first);
        Assert.Equal(7, repository.Get<DefenseRecord>().Single(r => r.ID == first.ID).BestWave);

        await repository.Delete(second);
        Assert.Equal(first.ID, Assert.Single(await repository.GetAllAsync<DefenseRecord>()).ID);
    }

    /// <summary>
    /// A cache hit must hand out a fresh list instance: callers sort, filter and clear the result
    /// (the statistics page trims the trip list before aggregating), and that must never be able to
    /// damage the cached copy or leak into the next reader.
    /// </summary>
    [Fact]
    public async Task Ef_CachedReads_ReturnIndependentLists()
    {
        using var db = new SqliteTestDatabase();
        await AssertCachedReadsReturnIndependentLists(db.Repository);
    }

    /// <inheritdoc cref="Ef_CachedReads_ReturnIndependentLists"/>
    [Fact]
    public async Task LiteDb_CachedReads_ReturnIndependentLists()
    {
        await WithLiteDb(AssertCachedReadsReturnIndependentLists);
    }

    [Fact]
    public async Task LiteDb_UsesTheDeclaredKeyInsteadOfGeneratingOne()
    {
        // Regression: LiteDB keys an entity on the field named "_id", and only a property called "ID"
        // was mapped onto it. The [Key] on TripFingerprint.TripID was therefore ignored, LiteDB invented
        // an "_id" per insert, FindById never found anything and the trip library collected two
        // fingerprints per trip until the backfill crashed on the duplicates.
        await WithLiteDb(async repository =>
        {
            var fingerprint = new TripFingerprint
            {
                TripID = Guid.NewGuid(),
                DistanceMeters = 1000,
                Version = TripFingerprintBuilder.Version,
            };

            await repository.Add(fingerprint);

            var stored = Assert.Single(await repository.GetAllAsync<TripFingerprint>());
            Assert.Equal(fingerprint.TripID, stored.TripID);

            Assert.NotNull(await repository.GetByIdAsync<TripFingerprint>(fingerprint.TripID));

            // The second insert of the same key must fail loudly instead of quietly duplicating.
            await Assert.ThrowsAnyAsync<Exception>(() => repository.Add(fingerprint));
            Assert.Single(await repository.GetAllAsync<TripFingerprint>());

            // Delete resolves the key by attribute too; it used to throw for this entity.
            await repository.Delete(stored);
            Assert.Empty(await repository.GetAllAsync<TripFingerprint>());
        });
    }

    [Fact]
    public async Task LiteDb_RewritesFingerprintsStoredWithAnInventedKey()
    {
        // Regression: every fingerprint written before the [Key] mapping existed carries an
        // auto-generated ObjectId in "_id" with the real key left in the payload. Reading such a
        // collection threw InvalidCastException ("Unable to cast ObjectId to Guid"), so the whole
        // compare page failed on a library that had ever been indexed by the old build.
        await WithLegacyFingerprint(async (path, tripId) =>
        {
            var database = new LiteDatabase(path);
            try
            {
                var fingerprint = Assert.Single(await new LiteDbRepository(database).GetAllAsync<TripFingerprint>());

                Assert.Equal(tripId, fingerprint.TripID);
                Assert.Equal(1234d, fingerprint.DistanceMeters);
                Assert.Equal(TripFingerprintBuilder.Version, fingerprint.Version);

                // Rewritten onto the real key: addressable now, and no duplicate left behind.
                Assert.NotNull(await new LiteDbRepository(database).GetByIdAsync<TripFingerprint>(tripId));
                Assert.Single(database.Database.GetCollection("TripFingerprint").FindAll());
            }
            finally
            {
                database.Database.Dispose();
            }
        });
    }

    [Fact]
    public async Task LiteDb_KeepsTheKeyedRowWhenALegacyDuplicateExists()
    {
        // A trip indexed by the old build and again by the new one holds two rows for the same trip.
        // The keyed row is the current one, so the legacy duplicate is the one that has to go.
        await WithLegacyFingerprint(async (path, tripId) =>
        {
            using (var seeding = new global::LiteDB.LiteDatabase(path))
            {
                seeding.GetCollection("TripFingerprint").Insert(new BsonDocument
                {
                    ["_id"] = tripId,
                    ["DistanceMeters"] = 4321d,
                    ["Version"] = TripFingerprintBuilder.Version,
                });
            }

            var database = new LiteDatabase(path);
            try
            {
                var fingerprint = Assert.Single(await new LiteDbRepository(database).GetAllAsync<TripFingerprint>());

                Assert.Equal(4321d, fingerprint.DistanceMeters);
                Assert.Single(database.Database.GetCollection("TripFingerprint").FindAll());
            }
            finally
            {
                database.Database.Dispose();
            }
        });
    }

    /// <summary>
    /// Writes one fingerprint the way the pre-mapping build did — auto-generated ObjectId in "_id",
    /// key in the payload — and hands the closed file plus its trip id to <paramref name="assertions"/>.
    /// </summary>
    private static async Task WithLegacyFingerprint(Func<string, Guid, Task> assertions)
    {
        var path = Path.Combine(Path.GetTempPath(), $"unitracks-tests-{Guid.NewGuid():N}.litedb");
        var tripId = Guid.NewGuid();

        using (var seeding = new global::LiteDB.LiteDatabase(path))
        {
            seeding.GetCollection("TripFingerprint").Insert(new BsonDocument
            {
                ["_id"] = ObjectId.NewObjectId(),
                ["TripID"] = tripId,
                ["DistanceMeters"] = 1234d,
                ["Version"] = TripFingerprintBuilder.Version,
            });
        }

        try
        {
            await assertions(path, tripId);
        }
        finally
        {
            foreach (var leftover in new[] { path, path + "-log.db" })
            {
                try
                {
                    if (File.Exists(leftover))
                    {
                        File.Delete(leftover);
                    }
                }
                catch (IOException)
                {
                    // A leftover temp file must never fail a test run.
                }
            }
        }
    }

    private static async Task AssertCachedReadsReturnIndependentLists(IRepository repository)
    {
        await repository.Add(new DefenseRecord { ID = Guid.NewGuid(), BestWave = 3, BestScore = 300 });

        var first = (await repository.GetAllAsync<DefenseRecord>()).ToList();
        var second = (await repository.GetAllAsync<DefenseRecord>()).ToList();

        Assert.NotSame(first, second);
        Assert.Equal(first[0].ID, second[0].ID);

        first.Clear();
        Assert.Single(await repository.GetAllAsync<DefenseRecord>());
    }

    /// <summary>
    /// Runs the assertions against a <see cref="LiteDbRepository"/> backed by a temp file, so the
    /// iOS code path is exercised on the test machine as well.
    /// </summary>
    private static async Task WithLiteDb(Func<IRepository, Task> assertions)
    {
        var path = Path.Combine(Path.GetTempPath(), $"unitracks-tests-{Guid.NewGuid():N}.litedb");
        var database = new LiteDatabase(path);
        try
        {
            await assertions(new LiteDbRepository(database));
        }
        finally
        {
            database.Database.Dispose();
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // A leftover temp file must never fail a test run.
            }
        }
    }
}
