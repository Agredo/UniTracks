using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UniTracks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedTripTypeSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000036"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000037"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000038"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000039"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000045"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000046"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000047"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000048"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000049"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000051"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000052"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000053"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000054"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000055"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000056"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000057"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000058"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000059"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000060"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000061"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000062"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000063"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000064"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000065"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000066"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000067"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000068"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000069"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000070"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000071"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000072"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000073"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000074"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000075"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000076"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000077"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000078"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000079"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000080"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000081"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000082"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000083"));

            migrationBuilder.DeleteData(
                table: "TripTypes",
                keyColumn: "ID",
                keyValue: new Guid("00000000-0000-0000-0000-000000000084"));
        }
    }
}
