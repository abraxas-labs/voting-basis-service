// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class ChangeListUnionDescriptions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "SubListUnionDescription",
            table: "ProportionalElectionLists",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "jsonb");

        migrationBuilder.AlterColumn<string>(
            name: "ListUnionDescription",
            table: "ProportionalElectionLists",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "jsonb");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "SubListUnionDescription",
            table: "ProportionalElectionLists",
            type: "jsonb",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "ListUnionDescription",
            table: "ProportionalElectionLists",
            type: "jsonb",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");
    }
}
