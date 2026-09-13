using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260913222140_GermanTripTypeNames : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
            column: "Name",
            value: "Laufen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
            column: "Name",
            value: "Trailrunning");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
            column: "Name",
            value: "Gehen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
            column: "Name",
            value: "Wandern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
            column: "Name",
            value: "Radfahren");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
            column: "Name",
            value: "Mountainbiken");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
            column: "Name",
            value: "Gravelbike");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
            column: "Name",
            value: "E-Bike");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
            column: "Name",
            value: "E-Mountainbike");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
            column: "Name",
            value: "Velo");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
            column: "Name",
            value: "Skifahren");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
            column: "Name",
            value: "Snowboarden");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000013"),
            column: "Name",
            value: "Langlauf");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000014"),
            column: "Name",
            value: "Tourenski");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000015"),
            column: "Name",
            value: "Telemark");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000016"),
            column: "Name",
            value: "Schneeschuhwandern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000017"),
            column: "Name",
            value: "Alpinski");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000018"),
            column: "Name",
            value: "Schneeschuhtour");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000019"),
            column: "Name",
            value: "Skaten");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000020"),
            column: "Name",
            value: "Inline-Skaten");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
            column: "Name",
            value: "Rollschuhlaufen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
            column: "Name",
            value: "Schlittschuhlaufen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000023"),
            column: "Name",
            value: "Schwimmen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000024"),
            column: "Name",
            value: "Freiwasserschwimmen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000025"),
            column: "Name",
            value: "Schwimmbad");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000026"),
            column: "Name",
            value: "Bahnen schwimmen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000028"),
            column: "Name",
            value: "Kajak");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000029"),
            column: "Name",
            value: "Stand-Up-Paddling");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000030"),
            column: "Name",
            value: "Rudern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
            column: "Name",
            value: "Drachenboot");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
            column: "Name",
            value: "Segeln");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000033"),
            column: "Name",
            value: "Surfen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000034"),
            column: "Name",
            value: "Kitesurfen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000035"),
            column: "Name",
            value: "Windsurfen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000036"),
            column: "Name",
            value: "Wakeboarden");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000037"),
            column: "Name",
            value: "Wakesurfen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000038"),
            column: "Name",
            value: "Wasserski");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000039"),
            column: "Name",
            value: "Jetski");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000040"),
            column: "Name",
            value: "Tauchen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000041"),
            column: "Name",
            value: "Apnoetauchen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000043"),
            column: "Name",
            value: "Reiten");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000044"),
            column: "Name",
            value: "Klettern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000045"),
            column: "Name",
            value: "Bouldern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000046"),
            column: "Name",
            value: "Hallenklettern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000047"),
            column: "Name",
            value: "Felsklettern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000048"),
            column: "Name",
            value: "Eisklettern");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000049"),
            column: "Name",
            value: "Bergsteigen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000050"),
            column: "Name",
            value: "Klettersteig");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000052"),
            column: "Name",
            value: "Skateboarden");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000053"),
            column: "Name",
            value: "Longboarden");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000060"),
            column: "Name",
            value: "Tanzen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000061"),
            column: "Name",
            value: "Aerobic");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000062"),
            column: "Name",
            value: "Step-Aerobic");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000064"),
            column: "Name",
            value: "Indoor-Radfahren");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000065"),
            column: "Name",
            value: "Boxen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000066"),
            column: "Name",
            value: "Kickboxen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000067"),
            column: "Name",
            value: "Kampfsport");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000072"),
            column: "Name",
            value: "Ringen");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000074"),
            column: "Name",
            value: "Fußball");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000076"),
            column: "Name",
            value: "Beachvolleyball");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000078"),
            column: "Name",
            value: "Tischtennis");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
            column: "Name",
            value: "Run");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
            column: "Name",
            value: "Trail Run");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
            column: "Name",
            value: "Walk");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
            column: "Name",
            value: "Hiking");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
            column: "Name",
            value: "Cycling");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
            column: "Name",
            value: "Mountain Biking");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
            column: "Name",
            value: "Gravel Ride");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
            column: "Name",
            value: "E-Bike Ride");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
            column: "Name",
            value: "E-Mountainbike Ride");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
            column: "Name",
            value: "Velobike Ride");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
            column: "Name",
            value: "Skiing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000012"),
            column: "Name",
            value: "Snowboarding");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000013"),
            column: "Name",
            value: "Cross Country Skiing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000014"),
            column: "Name",
            value: "Backcountry Skiing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000015"),
            column: "Name",
            value: "Telemark Skiing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000016"),
            column: "Name",
            value: "Snowshoeing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000017"),
            column: "Name",
            value: "Alpine Skiing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000018"),
            column: "Name",
            value: "Snowshoe Hike");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000019"),
            column: "Name",
            value: "Skating");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000020"),
            column: "Name",
            value: "Inline Skating");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
            column: "Name",
            value: "Roller Skating");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
            column: "Name",
            value: "Ice Skating");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000023"),
            column: "Name",
            value: "Swimming");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000024"),
            column: "Name",
            value: "Open Water Swimming");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000025"),
            column: "Name",
            value: "Pool Swimming");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000026"),
            column: "Name",
            value: "Lap Swimming");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000028"),
            column: "Name",
            value: "Kayak");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000029"),
            column: "Name",
            value: "Stand Up Paddling");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000030"),
            column: "Name",
            value: "Rowing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000031"),
            column: "Name",
            value: "Dragon Boat");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000032"),
            column: "Name",
            value: "Sailing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000033"),
            column: "Name",
            value: "Surfing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000034"),
            column: "Name",
            value: "Kitesurfing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000035"),
            column: "Name",
            value: "Windsurfing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000036"),
            column: "Name",
            value: "Wakeboarding");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000037"),
            column: "Name",
            value: "Wakesurfing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000038"),
            column: "Name",
            value: "Water Skiing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000039"),
            column: "Name",
            value: "Jet Skiing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000040"),
            column: "Name",
            value: "Diving");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000041"),
            column: "Name",
            value: "Freediving");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000043"),
            column: "Name",
            value: "Horseback Riding");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000044"),
            column: "Name",
            value: "Climbing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000045"),
            column: "Name",
            value: "Bouldering");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000046"),
            column: "Name",
            value: "Indoor Climbing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000047"),
            column: "Name",
            value: "Outdoor Climbing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000048"),
            column: "Name",
            value: "Ice Climbing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000049"),
            column: "Name",
            value: "Mountaineering");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000050"),
            column: "Name",
            value: "Via Ferrata");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000052"),
            column: "Name",
            value: "Skateboarding");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000053"),
            column: "Name",
            value: "Longboarding");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000060"),
            column: "Name",
            value: "Dance");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000061"),
            column: "Name",
            value: "Aerobics");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000062"),
            column: "Name",
            value: "Step Aerobics");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000064"),
            column: "Name",
            value: "Indoor Cycling");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000065"),
            column: "Name",
            value: "Boxing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000066"),
            column: "Name",
            value: "Kickboxing");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000067"),
            column: "Name",
            value: "Martial Arts");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000072"),
            column: "Name",
            value: "Wrestling");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000074"),
            column: "Name",
            value: "Soccer");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000076"),
            column: "Name",
            value: "Beach Volleyball");

        migrationBuilder.UpdateData(
            table: "TripTypes",
            keyColumn: "ID",
            keyValue: new Guid("00000000-0000-0000-0000-000000000078"),
            column: "Name",
            value: "Table Tennis");
    }
}
