using UniTracks.Data.LiteDB;
using UniTracks.Data.Repository;
using UniTracks.Games.TowerDefense.Persistence;
using UniTracks.Models.Trip;
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
