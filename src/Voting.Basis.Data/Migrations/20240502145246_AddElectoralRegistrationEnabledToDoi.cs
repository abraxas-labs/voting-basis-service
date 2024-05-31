// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddElectoralRegistrationEnabledToDoi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ElectoralRegistrationEnabled",
            table: "CantonSettings");

        migrationBuilder.AddColumn<bool>(
            name: "ElectoralRegistrationEnabled",
            table: "DomainOfInfluenceSnapshots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ElectoralRegistrationEnabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ElectoralRegistrationEnabled",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ElectoralRegistrationEnabled",
            table: "DomainOfInfluences");

        migrationBuilder.AddColumn<bool>(
            name: "ElectoralRegistrationEnabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}
