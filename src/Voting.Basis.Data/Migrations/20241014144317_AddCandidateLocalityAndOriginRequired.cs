// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddCandidateLocalityAndOriginRequired : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_CandidateLocalityRequired",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_CandidateOriginRequired",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CandidateLocalityRequired",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CandidateOriginRequired",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CantonDefaults_CandidateLocalityRequired",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_CandidateOriginRequired",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CandidateLocalityRequired",
            table: "CantonSettings");

        migrationBuilder.DropColumn(
            name: "CandidateOriginRequired",
            table: "CantonSettings");
    }
}
