// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddAdditionalCandidateFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Audited",
            table: "SecondaryMajorityElectionCandidates",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Comment",
            table: "SecondaryMajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "WahlvorschlagDescription",
            table: "SecondaryMajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "Audited",
            table: "ProportionalElectionCandidates",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Comment",
            table: "ProportionalElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "WahlvorschlagDescription",
            table: "ProportionalElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "Audited",
            table: "MajorityElectionCandidates",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Comment",
            table: "MajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "WahlvorschlagDescription",
            table: "MajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_EnableAdditionalCandidateFields",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EnableAdditionalCandidateFields",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Audited",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Comment",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "WahlvorschlagDescription",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Audited",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Comment",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "WahlvorschlagDescription",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Audited",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Comment",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "WahlvorschlagDescription",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_EnableAdditionalCandidateFields",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "EnableAdditionalCandidateFields",
            table: "CantonSettings");
    }
}
