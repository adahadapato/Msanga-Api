using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Msanga.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261114021605_SchoolMemberships : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CommunityMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FromYear = table.Column<int>(type: "int", nullable: false),
                ToYear = table.Column<int>(type: "int", nullable: false),
                Finished = table.Column<bool>(type: "bit", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommunityMemberships", x => x.Id);
                table.CheckConstraint("CK_CommunityMemberships_Years", "[FromYear] >= 1940 AND [ToYear] >= [FromYear] AND [ToYear] - [FromYear] < 12");
                table.ForeignKey(
                    name: "FK_CommunityMemberships_Communities_CommunityId",
                    column: x => x.CommunityId,
                    principalTable: "Communities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CommunityMemberships_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CommunityMemberships_CommunityId_ToYear",
            table: "CommunityMemberships",
            columns: new[] { "CommunityId", "ToYear" });

        migrationBuilder.CreateIndex(
            name: "IX_CommunityMemberships_MemberId_CommunityId",
            table: "CommunityMemberships",
            columns: new[] { "MemberId", "CommunityId" },
            unique: true,
            filter: "[IsDeleted] = 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CommunityMemberships");
    }
}
