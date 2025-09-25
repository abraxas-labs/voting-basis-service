// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddECollectingInitiativeAndReferendumProperties : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ECollectingMaxElectronicSignaturePercent",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingMinSignatureCount",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingMaxElectronicSignaturePercent",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "ECollectingMinSignatureCount",
            table: "DomainOfInfluences");

        migrationBuilder.AddColumn<int>(
            name: "ECollectingInitiativeMaxElectronicSignaturePercent",
            table: "DomainOfInfluenceSnapshots",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingInitiativeMinSignatureCount",
            table: "DomainOfInfluenceSnapshots",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingInitiativeNumberOfMembersCommittee",
            table: "DomainOfInfluenceSnapshots",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingReferendumMaxElectronicSignaturePercent",
            table: "DomainOfInfluenceSnapshots",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingReferendumMinSignatureCount",
            table: "DomainOfInfluenceSnapshots",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingInitiativeMaxElectronicSignaturePercent",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingInitiativeMinSignatureCount",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingInitiativeNumberOfMembersCommittee",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingReferendumMaxElectronicSignaturePercent",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingReferendumMinSignatureCount",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ECollectingInitiativeMaxElectronicSignaturePercent",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingInitiativeMinSignatureCount",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingInitiativeNumberOfMembersCommittee",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingReferendumMaxElectronicSignaturePercent",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingReferendumMinSignatureCount",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingInitiativeMaxElectronicSignaturePercent",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "ECollectingInitiativeMinSignatureCount",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "ECollectingInitiativeNumberOfMembersCommittee",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "ECollectingReferendumMaxElectronicSignaturePercent",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "ECollectingReferendumMinSignatureCount",
            table: "DomainOfInfluences");

        migrationBuilder.AddColumn<int>(
            name: "ECollectingMaxElectronicSignaturePercent",
            table: "DomainOfInfluenceSnapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingMinSignatureCount",
            table: "DomainOfInfluenceSnapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingMaxElectronicSignaturePercent",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECollectingMinSignatureCount",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
