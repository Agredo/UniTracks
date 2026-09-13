using System.ComponentModel.DataAnnotations;
using System.Reflection;
using LiteDB;
using UniTracks.Models.Comparison;
using UniTracks.Models.Location;
using LDB = LiteDB.LiteDatabase;

namespace UniTracks.Data.LiteDB;

public class LiteDatabase : ILiteDatabase
{
    public LDB Database { get; }
    public ILiteCollection<Location> Locations { get; }
    public string DatabasePath { get; }

    public LiteDatabase(string databasePath)
    {
        DatabasePath = databasePath;

        // Map our Guid "ID" properties onto LiteDB's "_id" field so Insert/Update/FindById
        // behave predictably for the top-level entities (Trip, User, ...). Without this LiteDB
        // auto-generates an "_id" that is never written back to the entity instance, which
        // breaks updates made through the same object.
        //
        // Entities that name their key differently but declare it with [Key] (as EF Core requires
        // and uses) have to be mapped too: with an auto-generated "_id" every Insert wrote a new
        // document, FindById never found anything, and a trip ended up with as many fingerprints
        // as the background indexer had runs.
        var mapper = BsonMapper.Global;
        mapper.ResolveMember = (type, member, memberMapper) =>
        {
            if (member.Name == "ID" || member.GetCustomAttribute<KeyAttribute>() is not null)
            {
                memberMapper.FieldName = "_id";
            }
        };

        Database = new LDB(databasePath);
        Locations = Database.GetCollection<Location>();

        RepairLegacyKeys(Database);
    }

    /// <summary>
    /// Entity types that were persisted before the key mapping above existed.
    /// <para>
    /// Their documents carry an auto-generated <c>ObjectId</c> in "_id" with the real key left in the
    /// payload. Once the key is mapped onto "_id", those documents cannot be read at all — the mapper
    /// tries to put an <c>ObjectId</c> into a <see cref="Guid"/> — so every read of the collection used
    /// to throw. They are rewritten here with the proper key, and the duplicates the missing mapping
    /// produced are collapsed, because an unusable document is worse than a rebuilt one: this data is
    /// derived and can be recreated from the trips.
    /// </para>
    /// </summary>
    private static readonly Type[] LegacyKeyedTypes = { typeof(TripFingerprint) };

    private static void RepairLegacyKeys(LDB database)
    {
        foreach (var type in LegacyKeyedTypes)
        {
            RepairLegacyKeys(database, type);
        }
    }

    private static void RepairLegacyKeys(LDB database, Type type)
    {
        var key = type.GetProperties().FirstOrDefault(property =>
            property.PropertyType == typeof(Guid)
            && property.GetCustomAttribute<KeyAttribute>() is not null);

        if (key is null)
        {
            return;
        }

        var collection = database.GetCollection(type.Name);
        var deletions = new List<BsonValue>();

        // Group by the identity a document *means*, not by where it currently keeps it: rows written
        // before the mapping above carry the key in the payload and an auto-generated "_id".
        var kept = new Dictionary<Guid, BsonDocument>();

        foreach (var document in collection.FindAll())
        {
            var stored = document["_id"];
            var payload = document.ContainsKey(key.Name) ? document[key.Name] : BsonValue.Null;
            var identity = stored.Type == BsonType.Guid ? stored.AsGuid
                : payload.Type == BsonType.Guid ? payload.AsGuid
                : (Guid?)null;

            if (identity is null)
            {
                // Neither in "_id" nor in the payload, so the row can be neither addressed nor
                // deserialised. Fingerprints are derived and rebuilt from the trip, hence dropping it.
                deletions.Add(stored);
                continue;
            }

            if (!kept.TryGetValue(identity.Value, out var existing))
            {
                kept[identity.Value] = document;
                continue;
            }

            // The missing mapping turned every write into an insert, so one trip can hold several rows.
            // Prefer the row already stored under the key, otherwise the newer derivation — ObjectId is
            // monotonic, so the highest timestamp is the most recent one.
            var keepNew = existing["_id"].Type == BsonType.Guid
                ? false
                : stored.Type == BsonType.Guid || Age(stored) > Age(existing["_id"]);

            if (keepNew)
            {
                deletions.Add(existing["_id"]);
                kept[identity.Value] = document;
            }
            else
            {
                deletions.Add(stored);
            }
        }

        foreach (var (identity, document) in kept)
        {
            if (document["_id"].Type == BsonType.Guid)
            {
                continue;
            }

            // Rewrite in place: the key moves from the payload into "_id", the content stays untouched.
            var stored = document["_id"];
            document.Remove(key.Name);
            document["_id"] = identity;
            collection.Insert(document);
            deletions.Add(stored);
        }

        foreach (var deletion in deletions)
        {
            collection.Delete(deletion);
        }
    }

    private static long Age(BsonValue id) =>
        id.Type == BsonType.ObjectId ? id.AsObjectId.Timestamp : long.MinValue;
}
