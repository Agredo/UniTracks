using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260906072822_AddTowerDefenseMapId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MapId",
            table: "DefenseRunProgresses",
            type: "TEXT",
            nullable: false,
            defaultValue: "waldwiese");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MapId",
            table: "DefenseRunProgresses");
    }
}
