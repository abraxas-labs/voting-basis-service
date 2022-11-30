// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class AddExternalPrintingCenterMessageType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ExternalPrintingCenter",
            table: "DomainOfInfluenceSnapshots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ExternalPrintingCenterEaiMessageType",
            table: "DomainOfInfluenceSnapshots",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "ExternalPrintingCenter",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ExternalPrintingCenterEaiMessageType",
            table: "DomainOfInfluences",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ExternalPrintingCenter",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ExternalPrintingCenterEaiMessageType",
            table: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropColumn(
            name: "ExternalPrintingCenter",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "ExternalPrintingCenterEaiMessageType",
            table: "DomainOfInfluences");
    }
}
