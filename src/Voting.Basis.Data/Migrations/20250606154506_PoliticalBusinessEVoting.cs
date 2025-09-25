// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class PoliticalBusinessEVoting : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EVotingApproved",
            table: "Votes",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingApproved",
            table: "SimplePoliticalBusiness",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingApproved",
            table: "SecondaryMajorityElections",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingApproved",
            table: "ProportionalElections",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingApproved",
            table: "MajorityElections",
            type: "boolean",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EVotingApproved",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "EVotingApproved",
            table: "SimplePoliticalBusiness");

        migrationBuilder.DropColumn(
            name: "EVotingApproved",
            table: "SecondaryMajorityElections");

        migrationBuilder.DropColumn(
            name: "EVotingApproved",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "EVotingApproved",
            table: "MajorityElections");
    }
}
