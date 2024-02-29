// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class AddDoiSapCustomerOrderNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SapCustomerOrderNumber",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "SapCustomerOrderNumber",
            table: "DomainOfInfluences",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SapCustomerOrderNumber",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "SapCustomerOrderNumber",
            table: "DomainOfInfluences");
    }
}
