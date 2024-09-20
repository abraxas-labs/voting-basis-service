// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class AddPoliticalBusinessSubType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "BusinessSubType",
            table: "SimplePoliticalBusiness",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE "SimplePoliticalBusiness" AS spb SET "BusinessSubType" = 1
            WHERE "BusinessType" = 1 AND EXISTS (
                SELECT 1 FROM "Votes" v INNER JOIN "Ballots" b ON v."Id" = b."VoteId"
                WHERE v."Id" = spb."Id" AND (v."Type" = 2 OR b."BallotType" = 2)
            )
        """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BusinessSubType",
            table: "SimplePoliticalBusiness");
    }
}
