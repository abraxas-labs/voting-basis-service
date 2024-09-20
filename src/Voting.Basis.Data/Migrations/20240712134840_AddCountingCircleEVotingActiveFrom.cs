// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddCountingCircleEVotingActiveFrom : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EVotingActiveFrom",
            table: "CountingCircleSnapshots",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EVotingActiveFrom",
            table: "CountingCircles",
            type: "date",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EVotingActiveFrom",
            table: "CountingCircleSnapshots");

        migrationBuilder.DropColumn(
            name: "EVotingActiveFrom",
            table: "CountingCircles");
    }
}
