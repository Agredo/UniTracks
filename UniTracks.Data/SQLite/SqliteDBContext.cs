using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Common.Contants;
using UniTracks.Models.Environment;
using UniTracks.Models.Health;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Models.User;
using UniTracks.Services.IO;

namespace UniTracks.Data.SQLite;

public class SqliteDBContext : DbContext
{
    //public DbSet<User> Users { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Weather> Weathers { get; set; }
    public DbSet<HeartRate> HeartRates { get; set; }
    public DbSet<Weight> Weights { get; set; }


    public DbContext Context => this;
    public string DatabasePath { get; }

    public SqliteDBContext()
    {
    }

    public SqliteDBContext(IFileSystem fileSystem)
    {
        DatabasePath = Path.Combine(fileSystem.AppDataDirectory, ApplicationConstants.SQliteDatabaseName);
        SQLitePCL.Batteries.Init();

        this.Database.EnsureCreated();
        this.Database.MigrateAsync();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Filename={DatabasePath}");
    }
}
