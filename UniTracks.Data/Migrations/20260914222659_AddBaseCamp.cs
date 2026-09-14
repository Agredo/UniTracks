using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260914222659_AddBaseCamp : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CampLogs",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                LastCollectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                TotalCollected = table.Column<int>(type: "INTEGER", nullable: false),
                BankedSupplies = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampLogs", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "CampModules",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                ModuleId = table.Column<string>(type: "TEXT", nullable: false),
                Level = table.Column<int>(type: "INTEGER", nullable: false),
                PurchasedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampModules", x => x.ID);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CampLogs");

        migrationBuilder.DropTable(
            name: "CampModules");
    }
}
