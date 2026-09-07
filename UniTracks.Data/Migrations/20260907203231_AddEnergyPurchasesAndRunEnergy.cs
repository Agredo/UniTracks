using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniTracks.Data.Migrations;

/// <inheritdoc />
public partial class _20260907203231_AddEnergyPurchasesAndRunEnergy : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Energy",
            table: "DefenseRunProgresses",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "EnergyPurchases",
            columns: table => new
            {
                ID = table.Column<Guid>(type: "TEXT", nullable: false),
                Energy = table.Column<int>(type: "INTEGER", nullable: false),
                Coins = table.Column<int>(type: "INTEGER", nullable: false),
                PurchasedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EnergyPurchases", x => x.ID);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EnergyPurchases");

        migrationBuilder.DropColumn(
            name: "Energy",
            table: "DefenseRunProgresses");
    }
}
