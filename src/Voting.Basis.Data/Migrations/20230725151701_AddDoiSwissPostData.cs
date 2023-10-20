// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class AddDoiSwissPostData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_FrankingLicenceNumber",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_InvoiceReferenceNumber",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_FrankingLicenceNumber",
            table: "DomainOfInfluences",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_InvoiceReferenceNumber",
            table: "DomainOfInfluences",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluences",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SwissPostData_FrankingLicenceNumber",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "SwissPostData_InvoiceReferenceNumber",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "SwissPostData_FrankingLicenceNumber",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "SwissPostData_InvoiceReferenceNumber",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "SwissPostData_OrderNumber",
            table: "DomainOfInfluences");
    }
}
