using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260906061534_TowerDefenseRunProgress : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DefenseRunProgresses",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Wave = table.Column<int>(type: "INTEGER", nullable: false),
                Lives = table.Column<int>(type: "INTEGER", nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: false),
                TowersJson = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DefenseRunProgresses", x => x.ID);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DefenseRunProgresses");
    }
}
