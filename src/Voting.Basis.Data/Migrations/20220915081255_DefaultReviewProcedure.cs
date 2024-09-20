// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class DefaultReviewProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Set the default value of the review procedure to electronically on all exisiting votes,
        // majority elections and proportional elections.
        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "MajorityElections");

        migrationBuilder.AddColumn<bool>(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "Votes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ReviewProcedure",
            table: "Votes",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<bool>(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "ProportionalElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ReviewProcedure",
            table: "ProportionalElections",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<bool>(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ReviewProcedure",
            table: "MajorityElections",
            type: "integer",
            nullable: false,
            defaultValue: 1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "MajorityElections");
    }
}
