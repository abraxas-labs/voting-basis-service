// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddContestEVotingApproval : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EVotingApprovalDueDate",
            table: "Contests",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EVotingApproved",
            table: "Contests",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EVotingApprovalDueDate",
            table: "Contests");

        migrationBuilder.DropColumn(
            name: "EVotingApproved",
            table: "Contests");
    }
}
