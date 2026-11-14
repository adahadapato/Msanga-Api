using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Msanga.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261113231818_Communities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Communities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                Level = table.Column<int>(type: "int", nullable: false),
                Town = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                AddedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SearchKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Communities", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CommunityAliases",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                SearchKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommunityAliases", x => x.Id);
                table.ForeignKey(
                    name: "FK_CommunityAliases_Communities_CommunityId",
                    column: x => x.CommunityId,
                    principalTable: "Communities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Communities_CountryCode",
            table: "Communities",
            column: "CountryCode");

        migrationBuilder.CreateIndex(
            name: "IX_Communities_SearchKey",
            table: "Communities",
            column: "SearchKey");

        migrationBuilder.CreateIndex(
            name: "IX_CommunityAliases_CommunityId_SearchKey",
            table: "CommunityAliases",
            columns: new[] { "CommunityId", "SearchKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CommunityAliases_SearchKey",
            table: "CommunityAliases",
            column: "SearchKey");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CommunityAliases");

        migrationBuilder.DropTable(
            name: "Communities");
    }
}
