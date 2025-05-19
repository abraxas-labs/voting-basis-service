// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddDoiFrankingLicenceAwayNumber : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_FrankingLicenceAwayNumber",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_FrankingLicenceAwayNumber",
            table: "DomainOfInfluences",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SwissPostData_FrankingLicenceAwayNumber",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "SwissPostData_FrankingLicenceAwayNumber",
            table: "DomainOfInfluences");
    }
}
