// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class RemoveSwissPostOrderNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluences");

        migrationBuilder.RenameColumn(
            name: "SwissPostData_FrankingLicenceNumber",
            table: "DomainOfInfluenceSnapshots",
            newName: "SwissPostData_FrankingLicenceReturnNumber");

        migrationBuilder.RenameColumn(
            name: "SwissPostData_FrankingLicenceNumber",
            table: "DomainOfInfluences",
            newName: "SwissPostData_FrankingLicenceReturnNumber");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "SwissPostData_FrankingLicenceReturnNumber",
            table: "DomainOfInfluenceSnapshots",
            newName: "SwissPostData_FrankingLicenceNumber");

        migrationBuilder.RenameColumn(
            name: "SwissPostData_FrankingLicenceReturnNumber",
            table: "DomainOfInfluences",
            newName: "SwissPostData_FrankingLicenceNumber");

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluences",
            type: "text",
            nullable: true);
    }
}
