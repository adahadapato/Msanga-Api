using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Msanga.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261114031909_Vouches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ConfirmedAt",
            table: "CommunityMemberships",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Vouches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VoucheeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vouches", x => x.Id);
                table.CheckConstraint("CK_Vouches_NotYourself", "[VoucherId] <> [VoucheeId]");
                table.ForeignKey(
                    name: "FK_Vouches_Communities_CommunityId",
                    column: x => x.CommunityId,
                    principalTable: "Communities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Vouches_Members_VoucheeId",
                    column: x => x.VoucheeId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Vouches_Members_VoucherId",
                    column: x => x.VoucherId,
                    principalTable: "Members",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_CommunityMemberships_CommunityId_Status",
            table: "CommunityMemberships",
            columns: new[] { "CommunityId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Vouches_CommunityId_VoucheeId",
            table: "Vouches",
            columns: new[] { "CommunityId", "VoucheeId" });

        migrationBuilder.CreateIndex(
            name: "IX_Vouches_CommunityId_VoucherId_VoucheeId",
            table: "Vouches",
            columns: new[] { "CommunityId", "VoucherId", "VoucheeId" },
            unique: true,
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_Vouches_VoucheeId",
            table: "Vouches",
            column: "VoucheeId");

        migrationBuilder.CreateIndex(
            name: "IX_Vouches_VoucherId",
            table: "Vouches",
            column: "VoucherId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Vouches");

        migrationBuilder.DropIndex(
            name: "IX_CommunityMemberships_CommunityId_Status",
            table: "CommunityMemberships");

        migrationBuilder.DropColumn(
            name: "ConfirmedAt",
            table: "CommunityMemberships");
    }
}
