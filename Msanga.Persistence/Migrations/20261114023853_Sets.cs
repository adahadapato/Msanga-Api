using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Msanga.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261114023853_Sets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ClassArm",
            table: "CommunityMemberships",
            type: "nvarchar(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Department",
            table: "CommunityMemberships",
            type: "nvarchar(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Hostel",
            table: "CommunityMemberships",
            type: "nvarchar(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "House",
            table: "CommunityMemberships",
            type: "nvarchar(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SetYear",
            table: "CommunityMemberships",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SetNaming",
            table: "Communities",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_CommunityMemberships_CommunityId_SetYear",
            table: "CommunityMemberships",
            columns: new[] { "CommunityId", "SetYear" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CommunityMemberships_CommunityId_SetYear",
            table: "CommunityMemberships");

        migrationBuilder.DropColumn(
            name: "ClassArm",
            table: "CommunityMemberships");

        migrationBuilder.DropColumn(
            name: "Department",
            table: "CommunityMemberships");

        migrationBuilder.DropColumn(
            name: "Hostel",
            table: "CommunityMemberships");

        migrationBuilder.DropColumn(
            name: "House",
            table: "CommunityMemberships");

        migrationBuilder.DropColumn(
            name: "SetYear",
            table: "CommunityMemberships");

        migrationBuilder.DropColumn(
            name: "SetNaming",
            table: "Communities");
    }
}
