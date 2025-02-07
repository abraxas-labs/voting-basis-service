// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddDoiPublishResults : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PublishResultsEnabled",
            table: "CantonSettings",
            newName: "ManualPublishResultsEnabled");

        migrationBuilder.AddColumn<bool>(
            name: "PublishResultsDisabled",
            table: "DomainOfInfluenceSnapshots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_DomainOfInfluencePublishResultsOptionEnabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "PublishResultsDisabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "DomainOfInfluencePublishResultsOptionEnabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PublishResultsDisabled",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_DomainOfInfluencePublishResultsOptionEnabled",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "PublishResultsDisabled",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluencePublishResultsOptionEnabled",
            table: "CantonSettings");

        migrationBuilder.RenameColumn(
            name: "ManualPublishResultsEnabled",
            table: "CantonSettings",
            newName: "PublishResultsEnabled");
    }
}
