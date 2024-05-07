using LiteDB;
using UniTracks.Models.Location;
using LDB = LiteDB.LiteDatabase;

namespace UniTracks.Data.LiteDB;

public interface ILiteDatabase
{
    LDB Database { get; }
    ILiteCollection<Location> Locations { get; }
}