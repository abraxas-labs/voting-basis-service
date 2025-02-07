// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddDomainOfInfluenceVcFlatRate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "VotingCardFlatRateDisabled",
            table: "DomainOfInfluenceSnapshots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "VotingCardFlatRateDisabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VotingCardFlatRateDisabled",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "VotingCardFlatRateDisabled",
            table: "DomainOfInfluences");
    }
}
