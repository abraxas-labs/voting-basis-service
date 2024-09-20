// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddEVotingCountingCircle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContestCountingCircleOptions");

        migrationBuilder.AddColumn<bool>(
            name: "EVoting",
            table: "CountingCircleSnapshots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EVoting",
            table: "CountingCircles",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EVoting",
            table: "CountingCircleSnapshots");

        migrationBuilder.DropColumn(
            name: "EVoting",
            table: "CountingCircles");

        migrationBuilder.CreateTable(
            name: "ContestCountingCircleOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                EVoting = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestCountingCircleOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleOptions_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleOptions_CountingCircles_CountingCircle~",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleOptions_ContestId",
            table: "ContestCountingCircleOptions",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleOptions_CountingCircleId_ContestId",
            table: "ContestCountingCircleOptions",
            columns: new[] { "CountingCircleId", "ContestId" },
            unique: true);
    }
}
