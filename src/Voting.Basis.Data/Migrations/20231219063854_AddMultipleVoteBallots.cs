// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class AddMultipleVoteBallots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Description",
            table: "Ballots");

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_MultipleVoteBallotsEnabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MultipleVoteBallotsEnabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CantonDefaults_MultipleVoteBallotsEnabled",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "MultipleVoteBallotsEnabled",
            table: "CantonSettings");

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "Ballots",
            type: "jsonb",
            nullable: false,
            defaultValue: string.Empty);
    }
}
