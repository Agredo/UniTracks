using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260906185956_AddTowerDefenseBestClear : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "BestClearScore",
            table: "DefenseRunProgresses",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "BestClearWave",
            table: "DefenseRunProgresses",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BestClearScore",
            table: "DefenseRunProgresses");

        migrationBuilder.DropColumn(
            name: "BestClearWave",
            table: "DefenseRunProgresses");
    }
}
