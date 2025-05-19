// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddPoliticalAssemblyState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ArchivePer",
            table: "PoliticalAssemblies",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PastLockPer",
            table: "PoliticalAssemblies",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<int>(
            name: "State",
            table: "PoliticalAssemblies",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_PoliticalAssemblies_State_DomainOfInfluenceId",
            table: "PoliticalAssemblies",
            columns: new[] { "State", "DomainOfInfluenceId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PoliticalAssemblies_State_DomainOfInfluenceId",
            table: "PoliticalAssemblies");

        migrationBuilder.DropColumn(
            name: "ArchivePer",
            table: "PoliticalAssemblies");

        migrationBuilder.DropColumn(
            name: "PastLockPer",
            table: "PoliticalAssemblies");

        migrationBuilder.DropColumn(
            name: "State",
            table: "PoliticalAssemblies");
    }
}
