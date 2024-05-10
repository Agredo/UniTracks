using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeathersIntoTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TripID",
                table: "Weathers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Weathers_TripID",
                table: "Weathers",
                column: "TripID");

            migrationBuilder.AddForeignKey(
                name: "FK_Weathers_Trips_TripID",
                table: "Weathers",
                column: "TripID",
                principalTable: "Trips",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Weathers_Trips_TripID",
                table: "Weathers");

            migrationBuilder.DropIndex(
                name: "IX_Weathers_TripID",
                table: "Weathers");

            migrationBuilder.DropColumn(
                name: "TripID",
                table: "Weathers");
        }
    }
}
