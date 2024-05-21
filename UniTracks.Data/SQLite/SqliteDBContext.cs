using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Common.Contants;
using UniTracks.Common.ExtensionMethods;
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
    public DbSet<TripType> TripTypes { get; set; }
    public DbSet<User> Users { get; set; }

    public DbContext Context => this;
    public string DatabasePath { get; }

    public SqliteDBContext()
    {
    }

    public SqliteDBContext(IFileSystem fileSystem)
    {
        DatabasePath = Path.Combine(fileSystem.AppDataDirectory, ApplicationConstants.SQliteDatabaseName);
        SQLitePCL.Batteries.Init();

        this.Database.MigrateAsync().Await();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Added as Migration. See AddedUserAndTriTypeWithSeed

        //modelBuilder.Entity<TripType>().HasData(
        //    // Running
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Run", Description = "", Identifier = "run", Category = "running" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Trail Run", Description = "", Identifier = "trailrun", Category = "running" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Walk", Description = "", Identifier = "walk", Category = "running" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Hiking", Description = "", Identifier = "hiking", Category = "running" },

        //    // Cycling
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = "Cycling", Description = "", Identifier = "cycling", Category = "cycling" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = "Mountain Biking", Description = "", Identifier = "mountainbiking", Category = "cycling" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = "Gravel Ride", Description = "", Identifier = "gravelride", Category = "cycling" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = "E-Bike Ride", Description = "", Identifier = "ebikeride", Category = "cycling" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000009"), Name = "E-Mountainbike Ride", Description = "", Identifier = "emountainbikeride", Category = "cycling" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000010"), Name = "Velobike Ride", Description = "", Identifier = "velobikeride", Category = "cycling" },

        //    // Winter Sports
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000011"), Name = "Skiing", Description = "", Identifier = "skiing", Category = "winter sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000012"), Name = "Snowboarding", Description = "", Identifier = "snowboarding", Category = "winter sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000013"), Name = "Cross Country Skiing", Description = "", Identifier = "crosscountryskiing", Category = "winter sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000014"), Name = "Backcountry Skiing", Description = "", Identifier = "backcountryskiing", Category = "winter sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000015"), Name = "Telemark Skiing", Description = "", Identifier = "telemarkskiing", Category = "winter sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000016"), Name = "Snowshoeing", Description = "", Identifier = "snowshoeing", Category = "winter sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000017"), Name = "Alpine Skiing", Description = "", Identifier = "alpineskiing", Category = "winter sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000018"), Name = "Snowshoe Hike", Description = "", Identifier = "snowshoehike", Category = "winter sports" },

        //    // Skating
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000019"), Name = "Skating", Description = "", Identifier = "skating", Category = "skating" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000020"), Name = "Inline Skating", Description = "", Identifier = "inlineskating", Category = "skating" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000021"), Name = "Roller Skating", Description = "", Identifier = "rollerskating", Category = "skating" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000022"), Name = "Ice Skating", Description = "", Identifier = "iceskating", Category = "skating" },

        //    // Water Sports
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000023"), Name = "Swimming", Description = "", Identifier = "swimming", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000024"), Name = "Open Water Swimming", Description = "", Identifier = "openwaterswimming", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000025"), Name = "Pool Swimming", Description = "", Identifier = "poolswimming", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000026"), Name = "Lap Swimming", Description = "", Identifier = "lapswimming", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000027"), Name = "Kanu", Description = "", Identifier = "kanu", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000028"), Name = "Kayak", Description = "", Identifier = "kayak", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000029"), Name = "Stand Up Paddling", Description = "", Identifier = "standuppaddling", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000030"), Name = "Rowing", Description = "", Identifier = "rowing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000031"), Name = "Dragon Boat", Description = "", Identifier = "dragonboat", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000032"), Name = "Sailing", Description = "", Identifier = "sailing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000033"), Name = "Surfing", Description = "", Identifier = "surfing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000034"), Name = "Kitesurfing", Description = "", Identifier = "kitesurfing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000035"), Name = "Windsurfing", Description = "", Identifier = "windsurfing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000036"), Name = "Wakeboarding", Description = "", Identifier = "wakeboarding", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000037"), Name = "Wakesurfing", Description = "", Identifier = "wakesurfing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000038"), Name = "Water Skiing", Description = "", Identifier = "waterskiing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000039"), Name = "Jet Skiing", Description = "", Identifier = "jetskiing", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000040"), Name = "Diving", Description = "", Identifier = "diving", Category = "water sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000041"), Name = "Freediving", Description = "", Identifier = "freediving", Category = "water sports" },

        //    // Miscellaneous
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000042"), Name = "Golf", Description = "", Identifier = "golf", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000043"), Name = "Horseback Riding", Description = "", Identifier = "horsebackriding", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000044"), Name = "Climbing", Description = "", Identifier = "climbing", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000045"), Name = "Bouldering", Description = "", Identifier = "bouldering", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000046"), Name = "Indoor Climbing", Description = "", Identifier = "indoorclimbing", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000047"), Name = "Outdoor Climbing", Description = "", Identifier = "outdoorclimbing", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000048"), Name = "Ice Climbing", Description = "", Identifier = "iceclimbing", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000049"), Name = "Mountaineering", Description = "", Identifier = "mountaineering", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000050"), Name = "Via Ferrata", Description = "", Identifier = "viaferrata", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000051"), Name = "Canyoning", Description = "", Identifier = "canyoning", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000052"), Name = "Skateboarding", Description = "", Identifier = "skateboarding", Category = "miscellaneous" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000053"), Name = "Longboarding", Description = "", Identifier = "longboarding", Category = "miscellaneous" },

        //    // Fitness
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000054"), Name = "Fitness", Description = "", Identifier = "fitness", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000055"), Name = "Crossfit", Description = "", Identifier = "crossfit", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000056"), Name = "Yoga", Description = "", Identifier = "yoga", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000057"), Name = "Pilates", Description = "", Identifier = "pilates", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000058"), Name = "Barre", Description = "", Identifier = "barre", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000059"), Name = "Zumba", Description = "", Identifier = "zumba", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000060"), Name = "Dance", Description = "", Identifier = "dance", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000061"), Name = "Aerobics", Description = "", Identifier = "aerobics", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000062"), Name = "Step Aerobics", Description = "", Identifier = "stepaerobics", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000063"), Name = "Spinning", Description = "", Identifier = "spinning", Category = "fitness" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000064"), Name = "Indoor Cycling", Description = "", Identifier = "indoorcycling", Category = "fitness" },

        //    // Fighting Sports
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000065"), Name = "Boxing", Description = "", Identifier = "boxing", Category = "fighting sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000066"), Name = "Kickboxing", Description = "", Identifier = "kickboxing", Category = "fighting sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000067"), Name = "Martial Arts", Description = "", Identifier = "martialarts", Category = "fighting sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000068"), Name = "Taekwondo", Description = "", Identifier = "taekwondo", Category = "fighting sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000069"), Name = "Karate", Description = "", Identifier = "karate", Category = "fighting sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000070"), Name = "Judo", Description = "", Identifier = "judo", Category = "fighting sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000071"), Name = "Jiu Jitsu", Description = "", Identifier = "jiujitsu", Category = "fighting sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000072"), Name = "Wrestling", Description = "", Identifier = "wrestling", Category = "fighting sports" },

        //    // Ball Sports
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000073"), Name = "Football", Description = "", Identifier = "football", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000074"), Name = "Soccer", Description = "", Identifier = "soccer", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000075"), Name = "Volleyball", Description = "", Identifier = "volleyball", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000076"), Name = "Beach Volleyball", Description = "", Identifier = "beachvolleyball", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000077"), Name = "Tennis", Description = "", Identifier = "tennis", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000078"), Name = "Table Tennis", Description = "", Identifier = "tabletennis", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000079"), Name = "Badminton", Description = "", Identifier = "badminton", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000080"), Name = "Squash", Description = "", Identifier = "squash", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000081"), Name = "Racquetball", Description = "", Identifier = "racquetball", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000082"), Name = "Handball", Description = "", Identifier = "handball", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000083"), Name = "Basketball", Description = "", Identifier = "basketball", Category = "ball sports" },
        //    new TripType() { ID = Guid.Parse("00000000-0000-0000-0000-000000000084"), Name = "American Football", Description = "", Identifier = "americanfootball", Category = "ball sports" }
        //);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Filename={DatabasePath}");
    }
}
