// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddAutomaticBallotNumberGeneration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AutomaticBallotNumberGeneration",
            table: "Votes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE "Votes"
            SET "AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<bool>(
            name: "AutomaticBallotNumberGeneration",
            table: "ProportionalElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE "ProportionalElections"
            SET "AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<bool>(
            name: "AutomaticBallotNumberGeneration",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE "MajorityElections"
            SET "AutomaticBallotNumberGeneration" = TRUE
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AutomaticBallotNumberGeneration",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "AutomaticBallotNumberGeneration",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "AutomaticBallotNumberGeneration",
            table: "MajorityElections");
    }
}
