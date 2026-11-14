using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Msanga.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261114035126_WhereYouLive : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LivesInCity",
            table: "Members",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LivesInCountryCode",
            table: "Members",
            type: "char(2)",
            unicode: false,
            fixedLength: true,
            maxLength: 2,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Members_LivesInCountryCode",
            table: "Members",
            column: "LivesInCountryCode");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Members_LivesInCountryCode",
            table: "Members");

        migrationBuilder.DropColumn(
            name: "LivesInCity",
            table: "Members");

        migrationBuilder.DropColumn(
            name: "LivesInCountryCode",
            table: "Members");
    }
}
