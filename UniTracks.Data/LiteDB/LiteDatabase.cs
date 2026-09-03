using LiteDB;
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
        var mapper = BsonMapper.Global;
        mapper.ResolveMember = (type, member, memberMapper) =>
        {
            if (member.Name == "ID")
            {
                memberMapper.FieldName = "_id";
            }
        };

        Database = new LDB(databasePath);
        Locations = Database.GetCollection<Location>();
    }
}
