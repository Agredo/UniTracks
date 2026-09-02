using LiteDB;
using UniTracks.Models.Location;
using LDB = LiteDB.LiteDatabase;

namespace UniTracks.Data.LiteDB;

public class LiteDatabase : ILiteDatabase
{
    public LDB Database { get; }

    public ILiteCollection<Location> Locations { get; }

    public LiteDatabase(string databasePath)
    {
        Database = new LDB(databasePath);

        Locations = Database.GetCollection<Location>();
    }
}
