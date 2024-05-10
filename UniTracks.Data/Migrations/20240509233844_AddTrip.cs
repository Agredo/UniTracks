using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TripID",
                table: "Locations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Distance = table.Column<double>(type: "REAL", nullable: false),
                    AverageSpeed = table.Column<double>(type: "REAL", nullable: false),
                    MaxSpeed = table.Column<double>(type: "REAL", nullable: false),
                    MinSpeed = table.Column<double>(type: "REAL", nullable: false),
                    MaxAltitude = table.Column<double>(type: "REAL", nullable: false),
                    MinAltitude = table.Column<double>(type: "REAL", nullable: false),
                    MaxAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    MinAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    MaxSpeedAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    MinSpeedAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    MaxHeading = table.Column<double>(type: "REAL", nullable: false),
                    MinHeading = table.Column<double>(type: "REAL", nullable: false),
                    MaxHeadingAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    MinHeadingAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    TotalTime = table.Column<double>(type: "REAL", nullable: false),
                    MovingTime = table.Column<double>(type: "REAL", nullable: false),
                    StoppedTime = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TripID",
                table: "Locations",
                column: "TripID");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Trips_TripID",
                table: "Locations",
                column: "TripID",
                principalTable: "Trips",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Trips_TripID",
                table: "Locations");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Locations_TripID",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "TripID",
                table: "Locations");
        }
    }
}
