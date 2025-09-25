// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddECollectingEmail : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ECollectingEmail",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "ECollectingEmail",
            table: "DomainOfInfluences",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ECollectingEmail",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ECollectingEmail",
            table: "DomainOfInfluences");
    }
}
