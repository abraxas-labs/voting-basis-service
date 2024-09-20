// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddVariantsOnMultipleBallots : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Type",
            table: "Votes",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "OfficialDescription",
            table: "Ballots",
            type: "jsonb",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "ShortDescription",
            table: "Ballots",
            type: "jsonb",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<int>(
            name: "SubType",
            table: "Ballots",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Type",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "OfficialDescription",
            table: "Ballots");

        migrationBuilder.DropColumn(
            name: "ShortDescription",
            table: "Ballots");

        migrationBuilder.DropColumn(
            name: "SubType",
            table: "Ballots");
    }
}
