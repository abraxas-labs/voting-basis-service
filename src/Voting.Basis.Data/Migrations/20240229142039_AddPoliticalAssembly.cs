// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddPoliticalAssembly : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PoliticalAssemblyId",
            table: "EventLog",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "PoliticalAssemblies",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateTime>(type: "date", nullable: false),
                Description = table.Column<string>(type: "jsonb", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PoliticalAssemblies", x => x.Id);
                table.ForeignKey(
                    name: "FK_PoliticalAssemblies_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PoliticalAssemblies_DomainOfInfluenceId",
            table: "PoliticalAssemblies",
            column: "DomainOfInfluenceId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PoliticalAssemblies");

        migrationBuilder.DropColumn(
            name: "PoliticalAssemblyId",
            table: "EventLog");
    }
}
