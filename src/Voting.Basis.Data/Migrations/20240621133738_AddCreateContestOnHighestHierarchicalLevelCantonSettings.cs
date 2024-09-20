// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddCreateContestOnHighestHierarchicalLevelCantonSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_CreateContestOnHighestHierarchicalLevelEnabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CreateContestOnHighestHierarchicalLevelEnabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CantonDefaults_CreateContestOnHighestHierarchicalLevelEnabled",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CreateContestOnHighestHierarchicalLevelEnabled",
            table: "CantonSettings");
    }
}
