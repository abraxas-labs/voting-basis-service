// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

/// <inheritdoc />
public partial class FixDoiPostData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE ""DomainOfInfluences"" 
SET ""SwissPostData_FrankingLicenceAwayNumber"" = '' 
WHERE ""SwissPostData_FrankingLicenceAwayNumber"" IS NULL
AND ""SwissPostData_FrankingLicenceReturnNumber"" IS NOT NULL
            ");

        migrationBuilder.Sql(@"
UPDATE ""DomainOfInfluenceSnapshots"" 
SET ""SwissPostData_FrankingLicenceAwayNumber"" = '' 
WHERE ""SwissPostData_FrankingLicenceAwayNumber"" IS NULL
AND ""SwissPostData_FrankingLicenceReturnNumber"" IS NOT NULL
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
