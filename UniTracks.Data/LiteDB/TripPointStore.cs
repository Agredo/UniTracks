using LiteDB;
using UniTracks.Data.Maintenance;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;

namespace UniTracks.Data.LiteDB;

/// <summary>
/// Owns where a trip's GPS points live.
/// <para>
/// They live in the <c>Location</c> collection, beside the trip - the same layout EF Core uses with
/// its own Locations table - and not inside the trip document. That is what keeps the trip list
/// cheap: a recording produces roughly one GPS point per second, so a document that carries its
/// points costs about 4 ms per 1.000 points to parse, and every list read, every cache miss and
/// every trip update paid that price for data the list never shows.
/// </para>
/// <para>
/// Everything that concerns the points goes through here, so the layout is described in one place:
/// <see cref="PrepareForInsert"/> and <see cref="PrepareForUpdate"/> write a trip without them,
/// <see cref="AttachPoints"/> reads them back for the callers that ask for them, and
/// <see cref="RunAsync"/> moves the ones that are still embedded in older documents over.
/// </para>
/// </summary>
public sealed class TripPointStore : IDataMigration
{
    /// <summary>
    /// Trips paged per round in <see cref="RunAsync"/>. Paged instead of streamed, because a document
    /// is rewritten while the scan is running - an open enumerator over the same index must not be
    /// held across a write - and small enough that one page of old documents stays cheap in memory.
    /// </summary>
    private const int SplitBatchSize = 25;

    private readonly ILiteDatabase _liteDatabase;

    public TripPointStore(ILiteDatabase liteDatabase)
    {
        _liteDatabase = liteDatabase;
    }

    private ILiteCollection<Location> Points => _liteDatabase.Locations;

    private ILiteCollection<Trip> Trips => _liteDatabase.Database.GetCollection<Trip>();

    /// <summary>
    /// Prepares a trip for its first write. Points it carries are stored in the collection first, so
    /// a caller that hands over a whole graph at once (an importer, for example) loses nothing.
    /// </summary>
    public Trip PrepareForInsert(Trip trip)
    {
        if (trip.Locations is { Count: > 0 })
        {
            Store(trip.ID, trip.Locations);
        }

        return trip with { Locations = [] };
    }

    /// <summary>
    /// Prepares a trip for an update. Points the document still carries are stored before the
    /// header-only write drops them, and navigations the caller did not load keep their stored value
    /// - an update writes the trip's own values, exactly like the EF repository leaves untracked
    /// navigations alone.
    /// </summary>
    public Trip PrepareForUpdate(Trip trip)
    {
        var stored = Trips.FindById(trip.ID);

        if (stored is { Locations.Count: > 0 })
        {
            Store(stored.ID, stored.Locations);
        }

        return (trip with { Locations = [] }) with
        {
            // A type change arrives with the type itself; without one the stored type stays, unless
            // the type id was cleared, in which case a kept type would contradict it.
            TripType = trip.TripType ?? (trip.TripTypeId == stored?.TripTypeId ? stored?.TripType : null),
            HeartRates = trip.HeartRates ?? stored?.HeartRates,
            Weights = trip.Weights ?? stored?.Weights,
            Weather = trip.Weather ?? stored?.Weather,
        };
    }

    /// <summary>Attaches the stored points of one trip, oldest first.</summary>
    public Trip AttachPoints(Trip trip)
    {
        trip.Locations = Points
            .Find(point => point.TripID == trip.ID)
            .OrderBy(point => point.Timestamp)
            .ToList();

        return trip;
    }

    /// <summary>
    /// Attaches the stored points of several trips in one read. Used by the full reads that need the
    /// points of every trip (statistics, distance recalculation) instead of one query per trip.
    /// </summary>
    public IReadOnlyList<Trip> AttachPoints(IReadOnlyList<Trip> trips)
    {
        if (trips.Count == 0)
        {
            return trips;
        }

        var wanted = trips.Select(trip => trip.ID).ToHashSet();
        var byTrip = new Dictionary<Guid, List<Location>>();

        foreach (var point in Points.FindAll())
        {
            if (point.TripID is not { } tripId || !wanted.Contains(tripId))
            {
                continue;
            }

            if (!byTrip.TryGetValue(tripId, out var track))
            {
                byTrip[tripId] = track = new List<Location>();
            }

            track.Add(point);
        }

        foreach (var trip in trips)
        {
            trip.Locations = byTrip.TryGetValue(trip.ID, out var track)
                ? track.OrderBy(point => point.Timestamp).ToList()
                : [];
        }

        return trips;
    }

    /// <summary>
    /// Removes a deleted trip's points. Without this they would stay behind as orphans - embedded
    /// points used to disappear with the document they sat in.
    /// </summary>
    public void Remove(Guid tripId) => Points.DeleteMany(point => point.TripID == tripId);

    /// <summary>
    /// Moves the points of documents written by builds that still embedded them into the collection.
    /// Runs in the background and is a no-op once every trip passed through it.
    /// </summary>
    public Task RunAsync() => Task.Run(MoveEmbeddedPoints);

    private void MoveEmbeddedPoints()
    {
        var skip = 0;

        while (true)
        {
            // Materialised on purpose: the loop rewrites documents, which must not happen while an
            // enumerator over the same index is still open.
            var page = Trips.Find(Query.All(), skip, SplitBatchSize).ToList();
            if (page.Count == 0)
            {
                return;
            }

            foreach (var trip in page)
            {
                if (trip.Locations is not { Count: > 0 })
                {
                    // Written after the split, so there is nothing left to move.
                    continue;
                }

                Store(trip.ID, trip.Locations);
                Trips.Update(trip with { Locations = [] });
            }

            skip += page.Count;
        }
    }

    /// <summary>
    /// Writes points into the collection, in one transaction per trip. Upsert, not insert: the
    /// recording path stores every point as it arrives and also carries it in the trip it is
    /// building, so the same point reaches this code twice and the second write must not fail.
    /// <para>
    /// Batched on purpose: a transaction per point cost about half a millisecond each, which turned
    /// the migration of an existing library into a minute-long job.
    /// </para>
    /// </summary>
    private void Store(Guid tripId, IEnumerable<Location> points)
    {
        var batch = points
            .Select(point => point.TripID is null ? point with { TripID = tripId } : point)
            .ToList();

        if (batch.Count > 0)
        {
            Points.Upsert(batch);
        }
    }
}
