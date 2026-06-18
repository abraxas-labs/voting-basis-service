// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class RemovePreconfiguredContestDates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PreconfiguredContestDates");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PreconfiguredContestDates",
            columns: table => new
            {
                Id = table.Column<DateTime>(type: "date", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PreconfiguredContestDates", x => x.Id);
            });
    }
}
