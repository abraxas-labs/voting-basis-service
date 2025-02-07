// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddCantonSettingsSecondaryElectionsOnSameBallotSetting : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsOnSeparateBallot",
            table: "SecondaryMajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_SecondaryMajorityElectionOnSeparateBallot",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "SecondaryMajorityElectionOnSeparateBallot",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsOnSeparateBallot",
            table: "SecondaryMajorityElections");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_SecondaryMajorityElectionOnSeparateBallot",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "SecondaryMajorityElectionOnSeparateBallot",
            table: "CantonSettings");
    }
}
