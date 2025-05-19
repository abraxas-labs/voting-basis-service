// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddECollectingSettingsOnDois : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
