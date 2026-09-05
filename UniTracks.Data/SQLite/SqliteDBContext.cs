using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UniTracks.Games.Persistence;
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
    public DbSet<PlacedBuilding> PlacedBuildings { get; set; }

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

    /// <summary>
    /// Design-time / options-based constructor. Used by the <c>dotnet-ef</c> CLI (via
    /// <see cref="SqliteDesignTimeDbContextFactory"/>) and must NOT run migrations, otherwise the
    /// CLI throws <c>MigrationsNotFound</c> while generating the initial migration.
    /// </summary>
    public SqliteDBContext(DbContextOptions<SqliteDBContext> options) : base(options)
    {
        DatabasePath = string.Empty;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Trip>(e =>
        {
            e.HasOne(t => t.TripType)
                .WithMany()
                .HasForeignKey(t => t.TripTypeId);
        });

        // TripType seed catalog is read from the embedded triptypes.json at
        // model-build time and applied via HasData (flows into EF migrations).
        modelBuilder.Entity<TripType>().HasData(LoadTripTypeSeeds());
    }

    /// <summary>
    /// Reads the embedded <c>Data/triptypes.json</c> seed catalog into <see cref="TripType"/> instances.
    /// </summary>
    private static List<TripType> LoadTripTypeSeeds()
    {
        var assembly = typeof(SqliteDBContext).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("triptypes.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Embedded resource 'triptypes.json' not found in assembly {assembly.FullName}.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<TripType>>(reader.ReadToEnd(), options)
            ?? new List<TripType>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite($"Filename={DatabasePath}");
    }
}
