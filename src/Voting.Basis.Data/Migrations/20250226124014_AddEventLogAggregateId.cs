// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddEventLogAggregateId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "AggregateId",
            table: "EventLog",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<Guid>(
            name: "EntityId",
            table: "EventLog",
            type: "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AggregateId",
            table: "EventLog");
        migrationBuilder.DropColumn(
            name: "EntityId",
            table: "EventLog");
    }
}
