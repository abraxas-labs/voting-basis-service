// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddMajorityElectionPartyLongToCandidates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Party",
            table: "SecondaryMajorityElectionCandidates",
            newName: "PartyShortDescription");

        migrationBuilder.RenameColumn(
            name: "Party",
            table: "MajorityElectionCandidates",
            newName: "PartyShortDescription");

        migrationBuilder.AddColumn<string>(
            name: "PartyLongDescription",
            table: "SecondaryMajorityElectionCandidates",
            type: "jsonb",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "PartyLongDescription",
            table: "MajorityElectionCandidates",
            type: "jsonb",
            nullable: false,
            defaultValue: string.Empty);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PartyLongDescription",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "PartyLongDescription",
            table: "MajorityElectionCandidates");

        migrationBuilder.RenameColumn(
            name: "PartyShortDescription",
            table: "SecondaryMajorityElectionCandidates",
            newName: "Party");

        migrationBuilder.RenameColumn(
            name: "PartyShortDescription",
            table: "MajorityElectionCandidates",
            newName: "Party");
    }
}
