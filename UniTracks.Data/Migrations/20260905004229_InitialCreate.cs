using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260905004229_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PlacedBuildings",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                BuildingId = table.Column<string>(type: "TEXT", nullable: false),
                X = table.Column<int>(type: "INTEGER", nullable: false),
                Y = table.Column<int>(type: "INTEGER", nullable: false),
                PlacedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlacedBuildings", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "TripTypes",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Identifier = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Category = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TripTypes", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: true),
                Email = table.Column<string>(type: "TEXT", nullable: true),
                Password = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                ProfilePicture = table.Column<string>(type: "TEXT", nullable: true),
                Height = table.Column<double>(type: "REAL", nullable: true),
                BloodGroup = table.Column<string>(type: "TEXT", nullable: true),
                MedicalConditions = table.Column<string>(type: "TEXT", nullable: true),
                Medications = table.Column<string>(type: "TEXT", nullable: true),
                Allergies = table.Column<string>(type: "TEXT", nullable: true),
                EmergencyContact = table.Column<string>(type: "TEXT", nullable: true),
                EmergencyContactNumber = table.Column<string>(type: "TEXT", nullable: true),
                EmergencyContactEmail = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "Trips",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: true),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                EndTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Distance = table.Column<double>(type: "REAL", nullable: true),
                AverageSpeed = table.Column<double>(type: "REAL", nullable: true),
                MaxSpeed = table.Column<double>(type: "REAL", nullable: true),
                MinSpeed = table.Column<double>(type: "REAL", nullable: true),
                MaxAltitude = table.Column<double>(type: "REAL", nullable: true),
                MinAltitude = table.Column<double>(type: "REAL", nullable: true),
                MaxAccuracy = table.Column<double>(type: "REAL", nullable: true),
                MinAccuracy = table.Column<double>(type: "REAL", nullable: true),
                MaxSpeedAccuracy = table.Column<double>(type: "REAL", nullable: true),
                MinSpeedAccuracy = table.Column<double>(type: "REAL", nullable: true),
                MaxHeading = table.Column<double>(type: "REAL", nullable: true),
                MinHeading = table.Column<double>(type: "REAL", nullable: true),
                MaxHeadingAccuracy = table.Column<double>(type: "REAL", nullable: true),
                MinHeadingAccuracy = table.Column<double>(type: "REAL", nullable: true),
                TotalTime = table.Column<double>(type: "REAL", nullable: true),
                MovingTime = table.Column<double>(type: "REAL", nullable: true),
                StoppedTime = table.Column<double>(type: "REAL", nullable: true),
                TripTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                UserID = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Trips", x => x.ID);
                table.ForeignKey(
                    name: "FK_Trips_TripTypes_TripTypeId",
                    column: x => x.TripTypeId,
                    principalTable: "TripTypes",
                    principalColumn: "ID");
                table.ForeignKey(
                    name: "FK_Trips_Users_UserID",
                    column: x => x.UserID,
                    principalTable: "Users",
                    principalColumn: "ID");
            });

        migrationBuilder.CreateTable(
            name: "HeartRates",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Rate = table.Column<double>(type: "REAL", nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                TripID = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HeartRates", x => x.ID);
                table.ForeignKey(
                    name: "FK_HeartRates_Trips_TripID",
                    column: x => x.TripID,
                    principalTable: "Trips",
                    principalColumn: "ID");
            });

        migrationBuilder.CreateTable(
            name: "Locations",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Latitude = table.Column<double>(type: "REAL", nullable: false),
                Longitude = table.Column<double>(type: "REAL", nullable: false),
                Altitude = table.Column<double>(type: "REAL", nullable: false),
                Accuracy = table.Column<double>(type: "REAL", nullable: false),
                Speed = table.Column<double>(type: "REAL", nullable: false),
                SpeedAccuracy = table.Column<double>(type: "REAL", nullable: false),
                Heading = table.Column<double>(type: "REAL", nullable: false),
                HeadingAccuracy = table.Column<double>(type: "REAL", nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                TripID = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Locations", x => x.ID);
                table.ForeignKey(
                    name: "FK_Locations_Trips_TripID",
                    column: x => x.TripID,
                    principalTable: "Trips",
                    principalColumn: "ID");
            });

        migrationBuilder.CreateTable(
            name: "Weights",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                WeightValue = table.Column<double>(type: "REAL", nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                TripID = table.Column<Guid>(type: "TEXT", nullable: true),
                UserID = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Weights", x => x.ID);
                table.ForeignKey(
                    name: "FK_Weights_Trips_TripID",
                    column: x => x.TripID,
                    principalTable: "Trips",
                    principalColumn: "ID");
                table.ForeignKey(
                    name: "FK_Weights_Users_UserID",
                    column: x => x.UserID,
                    principalTable: "Users",
                    principalColumn: "ID");
            });

        migrationBuilder.CreateTable(
            name: "Weathers",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Temperature = table.Column<double>(type: "REAL", nullable: false),
                Humidity = table.Column<double>(type: "REAL", nullable: false),
                Pressure = table.Column<double>(type: "REAL", nullable: false),
                WindSpeed = table.Column<double>(type: "REAL", nullable: false),
                WindDirection = table.Column<double>(type: "REAL", nullable: false),
                CloudCover = table.Column<double>(type: "REAL", nullable: false),
                LocationID = table.Column<Guid>(type: "TEXT", nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                TripID = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Weathers", x => x.ID);
                table.ForeignKey(
                    name: "FK_Weathers_Locations_LocationID",
                    column: x => x.LocationID,
                    principalTable: "Locations",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Weathers_Trips_TripID",
                    column: x => x.TripID,
                    principalTable: "Trips",
                    principalColumn: "ID");
            });

        migrationBuilder.InsertData(
            table: "TripTypes",
            columns: new[] { "ID", "Category", "Description", "Identifier", "Name" },
            values: new object[,]
            {
                { new Guid("00000000-0000-0000-0000-000000000001"), "running", "", "run", "Run" },
                { new Guid("00000000-0000-0000-0000-000000000002"), "running", "", "trailrun", "Trail Run" },
                { new Guid("00000000-0000-0000-0000-000000000003"), "running", "", "walk", "Walk" },
                { new Guid("00000000-0000-0000-0000-000000000004"), "running", "", "hiking", "Hiking" },
                { new Guid("00000000-0000-0000-0000-000000000005"), "cycling", "", "cycling", "Cycling" },
                { new Guid("00000000-0000-0000-0000-000000000006"), "cycling", "", "mountainbiking", "Mountain Biking" },
                { new Guid("00000000-0000-0000-0000-000000000007"), "cycling", "", "gravelride", "Gravel Ride" },
                { new Guid("00000000-0000-0000-0000-000000000008"), "cycling", "", "ebikeride", "E-Bike Ride" },
                { new Guid("00000000-0000-0000-0000-000000000009"), "cycling", "", "emountainbikeride", "E-Mountainbike Ride" },
                { new Guid("00000000-0000-0000-0000-000000000010"), "cycling", "", "velobikeride", "Velobike Ride" },
                { new Guid("00000000-0000-0000-0000-000000000011"), "winter sports", "", "skiing", "Skiing" },
                { new Guid("00000000-0000-0000-0000-000000000012"), "winter sports", "", "snowboarding", "Snowboarding" },
                { new Guid("00000000-0000-0000-0000-000000000013"), "winter sports", "", "crosscountryskiing", "Cross Country Skiing" },
                { new Guid("00000000-0000-0000-0000-000000000014"), "winter sports", "", "backcountryskiing", "Backcountry Skiing" },
                { new Guid("00000000-0000-0000-0000-000000000015"), "winter sports", "", "telemarkskiing", "Telemark Skiing" },
                { new Guid("00000000-0000-0000-0000-000000000016"), "winter sports", "", "snowshoeing", "Snowshoeing" },
                { new Guid("00000000-0000-0000-0000-000000000017"), "winter sports", "", "alpineskiing", "Alpine Skiing" },
                { new Guid("00000000-0000-0000-0000-000000000018"), "winter sports", "", "snowshoehike", "Snowshoe Hike" },
                { new Guid("00000000-0000-0000-0000-000000000019"), "skating", "", "skating", "Skating" },
                { new Guid("00000000-0000-0000-0000-000000000020"), "skating", "", "inlineskating", "Inline Skating" },
                { new Guid("00000000-0000-0000-0000-000000000021"), "skating", "", "rollerskating", "Roller Skating" },
                { new Guid("00000000-0000-0000-0000-000000000022"), "skating", "", "iceskating", "Ice Skating" },
                { new Guid("00000000-0000-0000-0000-000000000023"), "water sports", "", "swimming", "Swimming" },
                { new Guid("00000000-0000-0000-0000-000000000024"), "water sports", "", "openwaterswimming", "Open Water Swimming" },
                { new Guid("00000000-0000-0000-0000-000000000025"), "water sports", "", "poolswimming", "Pool Swimming" },
                { new Guid("00000000-0000-0000-0000-000000000026"), "water sports", "", "lapswimming", "Lap Swimming" },
                { new Guid("00000000-0000-0000-0000-000000000027"), "water sports", "", "kanu", "Kanu" },
                { new Guid("00000000-0000-0000-0000-000000000028"), "water sports", "", "kayak", "Kayak" },
                { new Guid("00000000-0000-0000-0000-000000000029"), "water sports", "", "standuppaddling", "Stand Up Paddling" },
                { new Guid("00000000-0000-0000-0000-000000000030"), "water sports", "", "rowing", "Rowing" },
                { new Guid("00000000-0000-0000-0000-000000000031"), "water sports", "", "dragonboat", "Dragon Boat" },
                { new Guid("00000000-0000-0000-0000-000000000032"), "water sports", "", "sailing", "Sailing" },
                { new Guid("00000000-0000-0000-0000-000000000033"), "water sports", "", "surfing", "Surfing" },
                { new Guid("00000000-0000-0000-0000-000000000034"), "water sports", "", "kitesurfing", "Kitesurfing" },
                { new Guid("00000000-0000-0000-0000-000000000035"), "water sports", "", "windsurfing", "Windsurfing" },
                { new Guid("00000000-0000-0000-0000-000000000036"), "water sports", "", "wakeboarding", "Wakeboarding" },
                { new Guid("00000000-0000-0000-0000-000000000037"), "water sports", "", "wakesurfing", "Wakesurfing" },
                { new Guid("00000000-0000-0000-0000-000000000038"), "water sports", "", "waterskiing", "Water Skiing" },
                { new Guid("00000000-0000-0000-0000-000000000039"), "water sports", "", "jetskiing", "Jet Skiing" },
                { new Guid("00000000-0000-0000-0000-000000000040"), "water sports", "", "diving", "Diving" },
                { new Guid("00000000-0000-0000-0000-000000000041"), "water sports", "", "freediving", "Freediving" },
                { new Guid("00000000-0000-0000-0000-000000000042"), "miscellaneous", "", "golf", "Golf" },
                { new Guid("00000000-0000-0000-0000-000000000043"), "miscellaneous", "", "horsebackriding", "Horseback Riding" },
                { new Guid("00000000-0000-0000-0000-000000000044"), "miscellaneous", "", "climbing", "Climbing" },
                { new Guid("00000000-0000-0000-0000-000000000045"), "miscellaneous", "", "bouldering", "Bouldering" },
                { new Guid("00000000-0000-0000-0000-000000000046"), "miscellaneous", "", "indoorclimbing", "Indoor Climbing" },
                { new Guid("00000000-0000-0000-0000-000000000047"), "miscellaneous", "", "outdoorclimbing", "Outdoor Climbing" },
                { new Guid("00000000-0000-0000-0000-000000000048"), "miscellaneous", "", "iceclimbing", "Ice Climbing" },
                { new Guid("00000000-0000-0000-0000-000000000049"), "miscellaneous", "", "mountaineering", "Mountaineering" },
                { new Guid("00000000-0000-0000-0000-000000000050"), "miscellaneous", "", "viaferrata", "Via Ferrata" },
                { new Guid("00000000-0000-0000-0000-000000000051"), "miscellaneous", "", "canyoning", "Canyoning" },
                { new Guid("00000000-0000-0000-0000-000000000052"), "miscellaneous", "", "skateboarding", "Skateboarding" },
                { new Guid("00000000-0000-0000-0000-000000000053"), "miscellaneous", "", "longboarding", "Longboarding" },
                { new Guid("00000000-0000-0000-0000-000000000054"), "fitness", "", "fitness", "Fitness" },
                { new Guid("00000000-0000-0000-0000-000000000055"), "fitness", "", "crossfit", "Crossfit" },
                { new Guid("00000000-0000-0000-0000-000000000056"), "fitness", "", "yoga", "Yoga" },
                { new Guid("00000000-0000-0000-0000-000000000057"), "fitness", "", "pilates", "Pilates" },
                { new Guid("00000000-0000-0000-0000-000000000058"), "fitness", "", "barre", "Barre" },
                { new Guid("00000000-0000-0000-0000-000000000059"), "fitness", "", "zumba", "Zumba" },
                { new Guid("00000000-0000-0000-0000-000000000060"), "fitness", "", "dance", "Dance" },
                { new Guid("00000000-0000-0000-0000-000000000061"), "fitness", "", "aerobics", "Aerobics" },
                { new Guid("00000000-0000-0000-0000-000000000062"), "fitness", "", "stepaerobics", "Step Aerobics" },
                { new Guid("00000000-0000-0000-0000-000000000063"), "fitness", "", "spinning", "Spinning" },
                { new Guid("00000000-0000-0000-0000-000000000064"), "fitness", "", "indoorcycling", "Indoor Cycling" },
                { new Guid("00000000-0000-0000-0000-000000000065"), "fighting sports", "", "boxing", "Boxing" },
                { new Guid("00000000-0000-0000-0000-000000000066"), "fighting sports", "", "kickboxing", "Kickboxing" },
                { new Guid("00000000-0000-0000-0000-000000000067"), "fighting sports", "", "martialarts", "Martial Arts" },
                { new Guid("00000000-0000-0000-0000-000000000068"), "fighting sports", "", "taekwondo", "Taekwondo" },
                { new Guid("00000000-0000-0000-0000-000000000069"), "fighting sports", "", "karate", "Karate" },
                { new Guid("00000000-0000-0000-0000-000000000070"), "fighting sports", "", "judo", "Judo" },
                { new Guid("00000000-0000-0000-0000-000000000071"), "fighting sports", "", "jiujitsu", "Jiu Jitsu" },
                { new Guid("00000000-0000-0000-0000-000000000072"), "fighting sports", "", "wrestling", "Wrestling" },
                { new Guid("00000000-0000-0000-0000-000000000073"), "ball sports", "", "football", "Football" },
                { new Guid("00000000-0000-0000-0000-000000000074"), "ball sports", "", "soccer", "Soccer" },
                { new Guid("00000000-0000-0000-0000-000000000075"), "ball sports", "", "volleyball", "Volleyball" },
                { new Guid("00000000-0000-0000-0000-000000000076"), "ball sports", "", "beachvolleyball", "Beach Volleyball" },
                { new Guid("00000000-0000-0000-0000-000000000077"), "ball sports", "", "tennis", "Tennis" },
                { new Guid("00000000-0000-0000-0000-000000000078"), "ball sports", "", "tabletennis", "Table Tennis" },
                { new Guid("00000000-0000-0000-0000-000000000079"), "ball sports", "", "badminton", "Badminton" },
                { new Guid("00000000-0000-0000-0000-000000000080"), "ball sports", "", "squash", "Squash" },
                { new Guid("00000000-0000-0000-0000-000000000081"), "ball sports", "", "racquetball", "Racquetball" },
                { new Guid("00000000-0000-0000-0000-000000000082"), "ball sports", "", "handball", "Handball" },
                { new Guid("00000000-0000-0000-0000-000000000083"), "ball sports", "", "basketball", "Basketball" },
                { new Guid("00000000-0000-0000-0000-000000000084"), "ball sports", "", "americanfootball", "American Football" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_HeartRates_TripID",
            table: "HeartRates",
            column: "TripID");

        migrationBuilder.CreateIndex(
            name: "IX_Locations_TripID",
            table: "Locations",
            column: "TripID");

        migrationBuilder.CreateIndex(
            name: "IX_Trips_TripTypeId",
            table: "Trips",
            column: "TripTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_Trips_UserID",
            table: "Trips",
            column: "UserID");

        migrationBuilder.CreateIndex(
            name: "IX_Weathers_LocationID",
            table: "Weathers",
            column: "LocationID");

        migrationBuilder.CreateIndex(
            name: "IX_Weathers_TripID",
            table: "Weathers",
            column: "TripID");

        migrationBuilder.CreateIndex(
            name: "IX_Weights_TripID",
            table: "Weights",
            column: "TripID");

        migrationBuilder.CreateIndex(
            name: "IX_Weights_UserID",
            table: "Weights",
            column: "UserID");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HeartRates");

        migrationBuilder.DropTable(
            name: "PlacedBuildings");

        migrationBuilder.DropTable(
            name: "Weathers");

        migrationBuilder.DropTable(
            name: "Weights");

        migrationBuilder.DropTable(
            name: "Locations");

        migrationBuilder.DropTable(
            name: "Trips");

        migrationBuilder.DropTable(
            name: "TripTypes");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
