// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddPartyToProprtionalElectionList : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PartyId",
            table: "ProportionalElectionLists",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionLists_PartyId",
            table: "ProportionalElectionLists",
            column: "PartyId");

        migrationBuilder.AddForeignKey(
            name: "FK_ProportionalElectionLists_DomainOfInfluenceParties_PartyId",
            table: "ProportionalElectionLists",
            column: "PartyId",
            principalTable: "DomainOfInfluenceParties",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProportionalElectionLists_DomainOfInfluenceParties_PartyId",
            table: "ProportionalElectionLists");

        migrationBuilder.DropIndex(
            name: "IX_ProportionalElectionLists_PartyId",
            table: "ProportionalElectionLists");

        migrationBuilder.DropColumn(
            name: "PartyId",
            table: "ProportionalElectionLists");
    }
}
