// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class AddDoiShippingVotingCardsToDeliveryAddress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "PrintData_ShippingVotingCardsToDeliveryAddress",
            table: "DomainOfInfluenceSnapshots",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "PrintData_ShippingVotingCardsToDeliveryAddress",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PrintData_ShippingVotingCardsToDeliveryAddress",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "PrintData_ShippingVotingCardsToDeliveryAddress",
            table: "DomainOfInfluences");
    }
}
