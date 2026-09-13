using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260913142900_AddTripFingerprints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TripFingerprints",
            columns: table => new
            {
                TripID = table.Column<Guid>(type: "TEXT", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                TripTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                TripCategory = table.Column<string>(type: "TEXT", nullable: false),
                TripIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                PointCount = table.Column<int>(type: "INTEGER", nullable: false),
                DistanceMeters = table.Column<double>(type: "REAL", nullable: false),
                ElapsedSeconds = table.Column<double>(type: "REAL", nullable: false),
                MovingSeconds = table.Column<double>(type: "REAL", nullable: false),
                AverageSpeedMetersPerSecond = table.Column<double>(type: "REAL", nullable: false),
                MaxSpeedMetersPerSecond = table.Column<double>(type: "REAL", nullable: false),
                ElevationGainMeters = table.Column<double>(type: "REAL", nullable: false),
                MinAltitude = table.Column<double>(type: "REAL", nullable: false),
                MaxAltitude = table.Column<double>(type: "REAL", nullable: false),
                MinLatitude = table.Column<double>(type: "REAL", nullable: false),
                MaxLatitude = table.Column<double>(type: "REAL", nullable: false),
                MinLongitude = table.Column<double>(type: "REAL", nullable: false),
                MaxLongitude = table.Column<double>(type: "REAL", nullable: false),
                CenterLatitude = table.Column<double>(type: "REAL", nullable: false),
                CenterLongitude = table.Column<double>(type: "REAL", nullable: false),
                StartCell = table.Column<string>(type: "TEXT", nullable: false),
                EndCell = table.Column<string>(type: "TEXT", nullable: false),
                StartArea = table.Column<string>(type: "TEXT", nullable: false),
                EndArea = table.Column<string>(type: "TEXT", nullable: false),
                RouteCells = table.Column<string>(type: "TEXT", nullable: false),
                PolylineLatitudes = table.Column<string>(type: "TEXT", nullable: false),
                PolylineLongitudes = table.Column<string>(type: "TEXT", nullable: false),
                EffortScore = table.Column<double>(type: "REAL", nullable: false),
                EquivalentDistanceMeters = table.Column<double>(type: "REAL", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TripFingerprints", x => x.TripID);
                table.ForeignKey(
                    name: "FK_TripFingerprints_Trips_TripID",
                    column: x => x.TripID,
                    principalTable: "Trips",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TripFingerprints");
    }
}
