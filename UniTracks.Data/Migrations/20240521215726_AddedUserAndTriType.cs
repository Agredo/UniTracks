using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserAndTriType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserID",
                table: "Weights",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserID",
                table: "Trips",
                type: "TEXT",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Weights_UserID",
                table: "Weights",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_UserID",
                table: "Trips",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Users_UserID",
                table: "Trips",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Weights_Users_UserID",
                table: "Weights",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Users_UserID",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Weights_Users_UserID",
                table: "Weights");

            migrationBuilder.DropTable(
                name: "TripTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Weights_UserID",
                table: "Weights");

            migrationBuilder.DropIndex(
                name: "IX_Trips_UserID",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "Weights");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "Trips");
        }
    }
}
