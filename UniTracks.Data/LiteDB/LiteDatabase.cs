using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Common.Contants;
using UniTracks.Models.Location;
using UniTracks.Services.IO;
using LDB = LiteDB.LiteDatabase;

namespace UniTracks.Data.LiteDB;

public class LiteDatabase : ILiteDatabase
{
    public LDB Database { get; }

    public ILiteCollection<Location> Locations { get; }

    public LiteDatabase(IFileSystem fileSystem)
    {
        Database = new LDB(Path.Combine(fileSystem.AppDataDirectory, ApplicationConstants.LiteDBName));

        Locations = Database.GetCollection<Location>();
    }
}
