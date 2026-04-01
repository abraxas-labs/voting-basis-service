// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddEVotingEverApprovedInfo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EVotingEverApproved",
            table: "Votes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingEverApproved",
            table: "SimplePoliticalBusiness",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingEverApproved",
            table: "SecondaryMajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingEverApproved",
            table: "ProportionalElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingEverApproved",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EVotingEverApproved",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "EVotingEverApproved",
            table: "SimplePoliticalBusiness");

        migrationBuilder.DropColumn(
            name: "EVotingEverApproved",
            table: "SecondaryMajorityElections");

        migrationBuilder.DropColumn(
            name: "EVotingEverApproved",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "EVotingEverApproved",
            table: "MajorityElections");
    }
}
