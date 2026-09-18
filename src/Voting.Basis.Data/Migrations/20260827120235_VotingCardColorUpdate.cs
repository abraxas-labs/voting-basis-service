// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class VotingCardColorUpdate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        UPDATE "DomainOfInfluences"
        SET "VotingCardColor" = 10
        WHERE "VotingCardColor" = 5
        """);
        migrationBuilder.Sql("""
        UPDATE "DomainOfInfluences"
        SET "VotingCardColor" = 9
        WHERE "VotingCardColor" = 7
        """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
        UPDATE "DomainOfInfluences"
        SET "VotingCardColor" = 5
        WHERE "VotingCardColor" = 10
        """);
        migrationBuilder.Sql("""
        UPDATE "DomainOfInfluences"
        SET "VotingCardColor" = 7
        WHERE "VotingCardColor" = 9
        """);
    }
}
