// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class MigrateProportionalElectionMandateAlgorithms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
                UPDATE ""ProportionalElections""
                SET ""MandateAlgorithm"" = 4
                WHERE ""MandateAlgorithm"" = 2;

                UPDATE ""ProportionalElections""
                SET ""MandateAlgorithm"" = 6
                WHERE ""MandateAlgorithm"" = 3;
            ");

        migrationBuilder.Sql(@"
                UPDATE ""CantonSettings""
                SET ""ProportionalElectionMandateAlgorithms"" = array_replace(""ProportionalElectionMandateAlgorithms"", 2, 4);

                UPDATE ""CantonSettings""
                SET ""ProportionalElectionMandateAlgorithms"" = array_replace(""ProportionalElectionMandateAlgorithms"", 3, 6);
            ");

        migrationBuilder.Sql(@"
                UPDATE ""DomainOfInfluences""
                SET ""CantonDefaults_ProportionalElectionMandateAlgorithms"" = array_replace(""CantonDefaults_ProportionalElectionMandateAlgorithms"", 2, 4);

                UPDATE ""DomainOfInfluences""
                SET ""CantonDefaults_ProportionalElectionMandateAlgorithms"" = array_replace(""CantonDefaults_ProportionalElectionMandateAlgorithms"", 3, 6);
            ");
    }
}
