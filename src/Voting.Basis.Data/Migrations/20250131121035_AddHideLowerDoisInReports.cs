﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddHideLowerDoisInReports : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "HideLowerDomainOfInfluencesInReports",
            table: "DomainOfInfluenceSnapshots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "HideLowerDomainOfInfluencesInReports",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HideLowerDomainOfInfluencesInReports",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "HideLowerDomainOfInfluencesInReports",
            table: "DomainOfInfluences");
    }
}
