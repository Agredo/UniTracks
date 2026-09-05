using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260905135105_TowerDefense : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DefenseRecords",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                BestWave = table.Column<int>(type: "INTEGER", nullable: false),
                BestScore = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DefenseRecords", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "TowerUnlocks",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                TowerId = table.Column<string>(type: "TEXT", nullable: false),
                PurchasedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TowerUnlocks", x => x.ID);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DefenseRecords");

        migrationBuilder.DropTable(
            name: "TowerUnlocks");
    }
}
