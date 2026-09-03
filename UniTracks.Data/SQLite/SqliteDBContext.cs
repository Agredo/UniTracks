using System.Data;
using Microsoft.EntityFrameworkCore;
using UniTracks.Models.Constants;
using UniTracks.Models.Environment;
using UniTracks.Models.Health;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Models.User;

namespace UniTracks.Data.SQLite;

public class SqliteDBContext : DbContext
{
    //public DbSet<User> Users { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Weather> Weathers { get; set; }
    public DbSet<HeartRate> HeartRates { get; set; }
    public DbSet<Weight> Weights { get; set; }
    public DbSet<TripType> TripTypes { get; set; }
    public DbSet<User> Users { get; set; }

    public DbContext Context => this;
    public string DatabasePath { get; }

    public SqliteDBContext()
    {
        DatabasePath = string.Empty;
    }

    public SqliteDBContext(string databasePath)
    {
        DatabasePath = databasePath;
        SQLitePCL.Batteries.Init();

        // EF Core migrations run fine at runtime on Android / Mac Catalyst / Windows (JIT).
        // They are NOT supported on iOS with NativeAOT, which is why iOS uses LiteDB instead.
        Database.Migrate();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TripType seed data is applied via EF migration: AddedTripTypeSeeds.

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Filename={DatabasePath}");
    }
}
