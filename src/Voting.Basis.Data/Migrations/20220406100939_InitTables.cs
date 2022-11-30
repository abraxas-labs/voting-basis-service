// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Basis.Data.Migrations;

public partial class InitTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CantonSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Canton = table.Column<int>(type: "integer", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                AuthorityName = table.Column<string>(type: "text", nullable: false),
                ProportionalElectionMandateAlgorithms = table.Column<List<int>>(type: "integer[]", nullable: false),
                MajorityElectionAbsoluteMajorityAlgorithm = table.Column<int>(type: "integer", nullable: false),
                MajorityElectionInvalidVotes = table.Column<bool>(type: "boolean", nullable: false),
                SwissAbroadVotingRight = table.Column<int>(type: "integer", nullable: false),
                SwissAbroadVotingRightDomainOfInfluenceTypes = table.Column<List<int>>(type: "integer[]", nullable: false),
                EnabledPoliticalBusinessUnionTypes = table.Column<List<int>>(type: "integer[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CantonSettings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CountingCirclesMergers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Merged = table.Column<bool>(type: "boolean", nullable: false),
                ActiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CopyFromCountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCirclesMergers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircleSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BasisId = table.Column<Guid>(type: "uuid", nullable: false),
                ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Bfs = table.Column<string>(type: "text", nullable: false),
                ContactPersonSameDuringEventAsAfter = table.Column<bool>(type: "boolean", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                Code = table.Column<string>(type: "text", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircleSnapshots", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceCountingCircleSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BasisId = table.Column<Guid>(type: "uuid", nullable: false),
                BasisDomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                BasisCountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                Inherited = table.Column<bool>(type: "boolean", nullable: false),
                ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceCountingCircleSnapshots", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceHierarchies",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<string>(type: "text", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ParentIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                ChildIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceHierarchies", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluencePermissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<string>(type: "text", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                IsParent = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluencePermissions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CantonDefaults_Canton = table.Column<int>(type: "integer", nullable: false),
                CantonDefaults_ProportionalElectionMandateAlgorithms = table.Column<List<int>>(type: "integer[]", nullable: false),
                CantonDefaults_MajorityElectionAbsoluteMajorityAlgorithm = table.Column<int>(type: "integer", nullable: false),
                CantonDefaults_MajorityElectionInvalidVotes = table.Column<bool>(type: "boolean", nullable: false),
                CantonDefaults_SwissAbroadVotingRight = table.Column<int>(type: "integer", nullable: false),
                CantonDefaults_EnabledPoliticalBusinessUnionTypes = table.Column<List<int>>(type: "integer[]", nullable: false),
                LogoRef = table.Column<string>(type: "text", nullable: true),
                Name = table.Column<string>(type: "text", nullable: false),
                ShortName = table.Column<string>(type: "text", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                AuthorityName = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Canton = table.Column<int>(type: "integer", nullable: false),
                ResponsibleForVotingCards = table.Column<bool>(type: "boolean", nullable: false),
                Bfs = table.Column<string>(type: "text", nullable: false),
                Code = table.Column<string>(type: "text", nullable: false),
                SortNumber = table.Column<int>(type: "integer", nullable: false),
                ContactPerson_FirstName = table.Column<string>(type: "text", nullable: false),
                ContactPerson_FamilyName = table.Column<string>(type: "text", nullable: false),
                ContactPerson_Phone = table.Column<string>(type: "text", nullable: false),
                ContactPerson_MobilePhone = table.Column<string>(type: "text", nullable: false),
                ContactPerson_Email = table.Column<string>(type: "text", nullable: false),
                ReturnAddress_AddressLine1 = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_AddressLine2 = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_Street = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_AddressAddition = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_ZipCode = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_City = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_Country = table.Column<string>(type: "text", nullable: true),
                PrintData_ShippingAway = table.Column<int>(type: "integer", nullable: true),
                PrintData_ShippingReturn = table.Column<int>(type: "integer", nullable: true),
                PrintData_ShippingMethod = table.Column<int>(type: "integer", nullable: true),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluences", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluences_DomainOfInfluences_ParentId",
                    column: x => x.ParentId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BasisId = table.Column<Guid>(type: "uuid", nullable: false),
                BasisParentId = table.Column<Guid>(type: "uuid", nullable: true),
                ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                ShortName = table.Column<string>(type: "text", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                AuthorityName = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Canton = table.Column<int>(type: "integer", nullable: false),
                ResponsibleForVotingCards = table.Column<bool>(type: "boolean", nullable: false),
                Bfs = table.Column<string>(type: "text", nullable: false),
                Code = table.Column<string>(type: "text", nullable: false),
                SortNumber = table.Column<int>(type: "integer", nullable: false),
                ContactPerson_FirstName = table.Column<string>(type: "text", nullable: false),
                ContactPerson_FamilyName = table.Column<string>(type: "text", nullable: false),
                ContactPerson_Phone = table.Column<string>(type: "text", nullable: false),
                ContactPerson_MobilePhone = table.Column<string>(type: "text", nullable: false),
                ContactPerson_Email = table.Column<string>(type: "text", nullable: false),
                ReturnAddress_AddressLine1 = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_AddressLine2 = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_Street = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_AddressAddition = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_ZipCode = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_City = table.Column<string>(type: "text", nullable: true),
                ReturnAddress_Country = table.Column<string>(type: "text", nullable: true),
                PrintData_ShippingAway = table.Column<int>(type: "integer", nullable: true),
                PrintData_ShippingReturn = table.Column<int>(type: "integer", nullable: true),
                PrintData_ShippingMethod = table.Column<int>(type: "integer", nullable: true),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceSnapshots", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EventLogTenant",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<string>(type: "text", nullable: false),
                TenantName = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventLogTenant", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EventLogUser",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                Username = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventLogUser", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EventProcessingStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CommitPosition = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                PreparePosition = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                EventNumber = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventProcessingStates", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PreconfiguredContestDates",
            columns: table => new
            {
                Id = table.Column<DateTime>(type: "date", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PreconfiguredContestDates", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CantonSettingsVotingCardChannels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VotingChannel = table.Column<int>(type: "integer", nullable: false),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                CantonSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CantonSettingsVotingCardChannels", x => x.Id);
                table.ForeignKey(
                    name: "FK_CantonSettingsVotingCardChannels_CantonSettings_CantonSetti~",
                    column: x => x.CantonSettingsId,
                    principalTable: "CantonSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                MergeTargetId = table.Column<Guid>(type: "uuid", nullable: true),
                MergeOriginId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "text", nullable: false),
                Bfs = table.Column<string>(type: "text", nullable: false),
                ContactPersonSameDuringEventAsAfter = table.Column<bool>(type: "boolean", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                Code = table.Column<string>(type: "text", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircles", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircles_CountingCirclesMergers_MergeOriginId",
                    column: x => x.MergeOriginId,
                    principalTable: "CountingCirclesMergers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_CountingCircles_CountingCirclesMergers_MergeTargetId",
                    column: x => x.MergeTargetId,
                    principalTable: "CountingCirclesMergers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "AuthoritySnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Phone = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                Street = table.Column<string>(type: "text", nullable: false),
                Zip = table.Column<string>(type: "text", nullable: false),
                City = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuthoritySnapshots", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuthoritySnapshots_CountingCircleSnapshots_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircleSnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircleContactPersonSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleDuringEventId = table.Column<Guid>(type: "uuid", nullable: true),
                CountingCircleAfterEventId = table.Column<Guid>(type: "uuid", nullable: true),
                FirstName = table.Column<string>(type: "text", nullable: false),
                FamilyName = table.Column<string>(type: "text", nullable: false),
                Phone = table.Column<string>(type: "text", nullable: false),
                MobilePhone = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircleContactPersonSnapshots", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircleContactPersonSnapshots_CountingCircleSnapsho~1",
                    column: x => x.CountingCircleDuringEventId,
                    principalTable: "CountingCircleSnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CountingCircleContactPersonSnapshots_CountingCircleSnapshot~",
                    column: x => x.CountingCircleAfterEventId,
                    principalTable: "CountingCircleSnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Contests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateTime>(type: "date", nullable: false),
                Description = table.Column<string>(type: "jsonb", nullable: false),
                EndOfTestingPhase = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ArchivePer = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PastLockPer = table.Column<DateTime>(type: "date", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                EVoting = table.Column<bool>(type: "boolean", nullable: false),
                EVotingFrom = table.Column<DateTime>(type: "date", nullable: true),
                EVotingTo = table.Column<DateTime>(type: "date", nullable: true),
                PreviousContestId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contests", x => x.Id);
                table.ForeignKey(
                    name: "FK_Contests_Contests_PreviousContestId",
                    column: x => x.PreviousContestId,
                    principalTable: "Contests",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Contests_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceParties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "jsonb", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceParties", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceParties_DomainOfInfluences_DomainOfInfluen~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ExportConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                EaiMessageType = table.Column<string>(type: "text", nullable: false),
                ExportKeys = table.Column<string[]>(type: "text[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExportConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExportConfigurations_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PlausibilisationConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlausibilisationConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlausibilisationConfigurations_DomainOfInfluences_DomainOfI~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EventLog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventContent = table.Column<string>(type: "text", nullable: false),
                EventName = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EventUserId = table.Column<Guid>(type: "uuid", nullable: false),
                EventTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: true),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: true),
                ContestId = table.Column<Guid>(type: "uuid", nullable: true),
                PoliticalBusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                PoliticalBusinessUnionId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventLog", x => x.Id);
                table.ForeignKey(
                    name: "FK_EventLog_EventLogTenant_EventTenantId",
                    column: x => x.EventTenantId,
                    principalTable: "EventLogTenant",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_EventLog_EventLogUser_EventUserId",
                    column: x => x.EventUserId,
                    principalTable: "EventLogUser",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Authorities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Phone = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                Street = table.Column<string>(type: "text", nullable: false),
                Zip = table.Column<string>(type: "text", nullable: false),
                City = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Authorities", x => x.Id);
                table.ForeignKey(
                    name: "FK_Authorities_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircleContactPersons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleDuringEventId = table.Column<Guid>(type: "uuid", nullable: true),
                CountingCircleAfterEventId = table.Column<Guid>(type: "uuid", nullable: true),
                FirstName = table.Column<string>(type: "text", nullable: false),
                FamilyName = table.Column<string>(type: "text", nullable: false),
                Phone = table.Column<string>(type: "text", nullable: false),
                MobilePhone = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircleContactPersons", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircleContactPersons_CountingCircles_CountingCircl~1",
                    column: x => x.CountingCircleDuringEventId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CountingCircleContactPersons_CountingCircles_CountingCircle~",
                    column: x => x.CountingCircleAfterEventId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceCountingCircles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                ComparisonCountOfVotersCategory = table.Column<int>(type: "integer", nullable: false),
                Inherited = table.Column<bool>(type: "boolean", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceCountingCircles", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceCountingCircles_CountingCircles_CountingCi~",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceCountingCircles_DomainOfInfluences_DomainO~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ContestCountingCircleOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                EVoting = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestCountingCircleOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleOptions_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleOptions_CountingCircles_CountingCircle~",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                MandateAlgorithm = table.Column<int>(type: "integer", nullable: false),
                IndividualEmptyBallotsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                CandidateCheckDigit = table.Column<bool>(type: "boolean", nullable: false),
                InvalidVotes = table.Column<bool>(type: "boolean", nullable: false),
                BallotBundleSize = table.Column<int>(type: "integer", nullable: false),
                BallotBundleSampleSize = table.Column<int>(type: "integer", nullable: false),
                AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: false),
                BallotNumberGeneration = table.Column<int>(type: "integer", nullable: false),
                AutomaticEmptyVoteCounting = table.Column<bool>(type: "boolean", nullable: false),
                EnforceEmptyVoteCountingForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                ResultEntry = table.Column<int>(type: "integer", nullable: false),
                EnforceResultEntryForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                ReportDomainOfInfluenceLevel = table.Column<int>(type: "integer", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "jsonb", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElections", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElections_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElections_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionUnions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionUnions", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionUnions_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                MandateAlgorithm = table.Column<int>(type: "integer", nullable: false),
                CandidateCheckDigit = table.Column<bool>(type: "boolean", nullable: false),
                BallotBundleSize = table.Column<int>(type: "integer", nullable: false),
                BallotBundleSampleSize = table.Column<int>(type: "integer", nullable: false),
                AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: false),
                BallotNumberGeneration = table.Column<int>(type: "integer", nullable: false),
                AutomaticEmptyVoteCounting = table.Column<bool>(type: "boolean", nullable: false),
                EnforceEmptyVoteCountingForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "jsonb", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElections", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElections_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElections_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnions_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SimplePoliticalBusiness",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessType = table.Column<int>(type: "integer", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "jsonb", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SimplePoliticalBusiness", x => x.Id);
                table.ForeignKey(
                    name: "FK_SimplePoliticalBusiness_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SimplePoliticalBusiness_DomainOfInfluences_DomainOfInfluenc~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Votes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportDomainOfInfluenceLevel = table.Column<int>(type: "integer", nullable: false),
                ResultAlgorithm = table.Column<int>(type: "integer", nullable: false),
                BallotBundleSampleSizePercent = table.Column<int>(type: "integer", nullable: false),
                AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: false),
                EnforceResultEntryForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                ResultEntry = table.Column<int>(type: "integer", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "jsonb", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Votes", x => x.Id);
                table.ForeignKey(
                    name: "FK_Votes_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Votes_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ComparisonCountOfVotersConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                ThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
                PlausibilisationConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComparisonCountOfVotersConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ComparisonCountOfVotersConfigurations_PlausibilisationConfi~",
                    column: x => x.PlausibilisationConfigurationId,
                    principalTable: "PlausibilisationConfigurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ComparisonVoterParticipationConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MainLevel = table.Column<int>(type: "integer", nullable: false),
                ComparisonLevel = table.Column<int>(type: "integer", nullable: false),
                ThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
                PlausibilisationConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComparisonVoterParticipationConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ComparisonVoterParticipationConfigurations_Plausibilisation~",
                    column: x => x.PlausibilisationConfigurationId,
                    principalTable: "PlausibilisationConfigurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ComparisonVotingChannelConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VotingChannel = table.Column<int>(type: "integer", nullable: false),
                ThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
                PlausibilisationConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComparisonVotingChannelConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ComparisonVotingChannelConfigurations_PlausibilisationConfi~",
                    column: x => x.PlausibilisationConfigurationId,
                    principalTable: "PlausibilisationConfigurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ElectionGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                PrimaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ElectionGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_ElectionGroups_MajorityElections_PrimaryMajorityElectionId",
                    column: x => x.PrimaryMajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionBallotGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionBallotGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroups_MajorityElections_MajorityElec~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                PoliticalFirstName = table.Column<string>(type: "text", nullable: false),
                PoliticalLastName = table.Column<string>(type: "text", nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "date", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                Occupation = table.Column<string>(type: "jsonb", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                OccupationTitle = table.Column<string>(type: "jsonb", nullable: false),
                Incumbent = table.Column<bool>(type: "boolean", nullable: false),
                ZipCode = table.Column<string>(type: "text", nullable: false),
                Locality = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                Party = table.Column<string>(type: "jsonb", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionCandidates_MajorityElections_MajorityElecti~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionUnionEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionUnionEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionUnionEntries_MajorityElections_MajorityElec~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionUnionEntries_MajorityElectionUnions_Majorit~",
                    column: x => x.MajorityElectionUnionId,
                    principalTable: "MajorityElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionLists",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderNumber = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "jsonb", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                BlankRowCount = table.Column<int>(type: "integer", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                CountOfCandidates = table.Column<int>(type: "integer", nullable: false),
                CandidateCountOk = table.Column<bool>(type: "boolean", nullable: false),
                ListUnionDescription = table.Column<string>(type: "jsonb", nullable: false),
                SubListUnionDescription = table.Column<string>(type: "jsonb", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionLists", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionLists_ProportionalElections_Proportiona~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionEntries_ProportionalElections_Prop~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionEntries_ProportionalElectionUnions~",
                    column: x => x.ProportionalElectionUnionId,
                    principalTable: "ProportionalElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionLists",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderNumber = table.Column<string>(type: "text", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                ProportionalElectionUnionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionLists", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionLists_ProportionalElectionUnions_P~",
                    column: x => x.ProportionalElectionUnionId,
                    principalTable: "ProportionalElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Ballots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                BallotType = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "jsonb", nullable: false),
                HasTieBreakQuestions = table.Column<bool>(type: "boolean", nullable: false),
                VoteId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ballots", x => x.Id);
                table.ForeignKey(
                    name: "FK_Ballots_Votes_VoteId",
                    column: x => x.VoteId,
                    principalTable: "Votes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                AllowedCandidates = table.Column<int>(type: "integer", nullable: false),
                PrimaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                ElectionGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "jsonb", nullable: false),
                ShortDescription = table.Column<string>(type: "jsonb", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElections", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElections_ElectionGroups_ElectionGroupId",
                    column: x => x.ElectionGroupId,
                    principalTable: "ElectionGroups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElections_MajorityElections_PrimaryMajorit~",
                    column: x => x.PrimaryMajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Accumulated = table.Column<bool>(type: "boolean", nullable: false),
                AccumulatedPosition = table.Column<int>(type: "integer", nullable: false),
                ProportionalElectionListId = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                Number = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                PoliticalFirstName = table.Column<string>(type: "text", nullable: false),
                PoliticalLastName = table.Column<string>(type: "text", nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "date", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                Occupation = table.Column<string>(type: "jsonb", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                OccupationTitle = table.Column<string>(type: "jsonb", nullable: false),
                Incumbent = table.Column<bool>(type: "boolean", nullable: false),
                ZipCode = table.Column<string>(type: "text", nullable: false),
                Locality = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidates_DomainOfInfluenceParties_Par~",
                    column: x => x.PartyId,
                    principalTable: "DomainOfInfluenceParties",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidates_ProportionalElectionLists_Pr~",
                    column: x => x.ProportionalElectionListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListUnions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "jsonb", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionRootListUnionId = table.Column<Guid>(type: "uuid", nullable: true),
                ProportionalElectionMainListId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListUnions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnions_ProportionalElectionLists_Pr~",
                    column: x => x.ProportionalElectionMainListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnions_ProportionalElectionListUnio~",
                    column: x => x.ProportionalElectionRootListUnionId,
                    principalTable: "ProportionalElectionListUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnions_ProportionalElections_Propor~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionListEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionUnionListId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionListId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionListEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionListEntries_ProportionalElectionLi~",
                    column: x => x.ProportionalElectionListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionListEntries_ProportionalElectionUn~",
                    column: x => x.ProportionalElectionUnionListId,
                    principalTable: "ProportionalElectionUnionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BallotQuestions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                Question = table.Column<string>(type: "jsonb", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotQuestions", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotQuestions_Ballots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "Ballots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TieBreakQuestions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                Question = table.Column<string>(type: "jsonb", nullable: false),
                Question1Number = table.Column<int>(type: "integer", nullable: false),
                Question2Number = table.Column<int>(type: "integer", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TieBreakQuestions", x => x.Id);
                table.ForeignKey(
                    name: "FK_TieBreakQuestions_Ballots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "Ballots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionBallotGroupEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BlankRowCount = table.Column<int>(type: "integer", nullable: false),
                BallotGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: true),
                SecondaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: true),
                IndividualCandidatesVoteCount = table.Column<int>(type: "integer", nullable: false),
                CountOfCandidates = table.Column<int>(type: "integer", nullable: false),
                CandidateCountOk = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionBallotGroupEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntries_MajorityElectionBallotGr~",
                    column: x => x.BallotGroupId,
                    principalTable: "MajorityElectionBallotGroups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntries_MajorityElections_Primar~",
                    column: x => x.PrimaryMajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntries_SecondaryMajorityElectio~",
                    column: x => x.SecondaryMajorityElectionId,
                    principalTable: "SecondaryMajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                Number = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                PoliticalFirstName = table.Column<string>(type: "text", nullable: false),
                PoliticalLastName = table.Column<string>(type: "text", nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "date", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                Occupation = table.Column<string>(type: "jsonb", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                OccupationTitle = table.Column<string>(type: "jsonb", nullable: false),
                Incumbent = table.Column<bool>(type: "boolean", nullable: false),
                ZipCode = table.Column<string>(type: "text", nullable: false),
                Locality = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                Party = table.Column<string>(type: "jsonb", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidates_MajorityElectionCandida~",
                    column: x => x.CandidateReferenceId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidates_SecondaryMajorityElecti~",
                    column: x => x.SecondaryMajorityElectionId,
                    principalTable: "SecondaryMajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListUnionEntries",
            columns: table => new
            {
                ProportionalElectionListId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionListUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                Id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListUnionEntries", x => new { x.ProportionalElectionListId, x.ProportionalElectionListUnionId });
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnionEntries_ProportionalElectionL~1",
                    column: x => x.ProportionalElectionListUnionId,
                    principalTable: "ProportionalElectionListUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnionEntries_ProportionalElectionLi~",
                    column: x => x.ProportionalElectionListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionBallotGroupEntryCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryElectionCandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                SecondaryElectionCandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                BallotGroupEntryId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionBallotGroupEntryCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntryCandidates_MajorityElectio~1",
                    column: x => x.PrimaryElectionCandidateId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntryCandidates_MajorityElection~",
                    column: x => x.BallotGroupEntryId,
                    principalTable: "MajorityElectionBallotGroupEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntryCandidates_SecondaryMajorit~",
                    column: x => x.SecondaryElectionCandidateId,
                    principalTable: "SecondaryMajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Authorities_CountingCircleId",
            table: "Authorities",
            column: "CountingCircleId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuthoritySnapshots_CountingCircleId",
            table: "AuthoritySnapshots",
            column: "CountingCircleId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestions_BallotId",
            table: "BallotQuestions",
            column: "BallotId");

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestions_Number_BallotId",
            table: "BallotQuestions",
            columns: new[] { "Number", "BallotId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Ballots_VoteId_Position",
            table: "Ballots",
            columns: new[] { "VoteId", "Position" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CantonSettings_Canton",
            table: "CantonSettings",
            column: "Canton",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CantonSettingsVotingCardChannels_CantonSettingsId_Valid_Vot~",
            table: "CantonSettingsVotingCardChannels",
            columns: new[] { "CantonSettingsId", "Valid", "VotingChannel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComparisonCountOfVotersConfigurations_PlausibilisationConfi~",
            table: "ComparisonCountOfVotersConfigurations",
            columns: new[] { "PlausibilisationConfigurationId", "Category" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComparisonVoterParticipationConfigurations_Plausibilisation~",
            table: "ComparisonVoterParticipationConfigurations",
            columns: new[] { "PlausibilisationConfigurationId", "MainLevel", "ComparisonLevel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComparisonVotingChannelConfigurations_PlausibilisationConfi~",
            table: "ComparisonVotingChannelConfigurations",
            columns: new[] { "PlausibilisationConfigurationId", "VotingChannel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleOptions_ContestId",
            table: "ContestCountingCircleOptions",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleOptions_CountingCircleId_ContestId",
            table: "ContestCountingCircleOptions",
            columns: new[] { "CountingCircleId", "ContestId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Contests_DomainOfInfluenceId",
            table: "Contests",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_Contests_PreviousContestId",
            table: "Contests",
            column: "PreviousContestId");

        migrationBuilder.CreateIndex(
            name: "IX_Contests_State_DomainOfInfluenceId",
            table: "Contests",
            columns: new[] { "State", "DomainOfInfluenceId" });

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleContactPersons_CountingCircleAfterEventId",
            table: "CountingCircleContactPersons",
            column: "CountingCircleAfterEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleContactPersons_CountingCircleDuringEventId",
            table: "CountingCircleContactPersons",
            column: "CountingCircleDuringEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleContactPersonSnapshots_CountingCircleAfterEve~",
            table: "CountingCircleContactPersonSnapshots",
            column: "CountingCircleAfterEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleContactPersonSnapshots_CountingCircleDuringEv~",
            table: "CountingCircleContactPersonSnapshots",
            column: "CountingCircleDuringEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircles_MergeOriginId",
            table: "CountingCircles",
            column: "MergeOriginId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircles_MergeTargetId",
            table: "CountingCircles",
            column: "MergeTargetId");

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleSnapshots_BasisId",
            table: "CountingCircleSnapshots",
            column: "BasisId");

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleSnapshots_ValidFrom",
            table: "CountingCircleSnapshots",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircles_CountingCircleId_DomainOfI~",
            table: "DomainOfInfluenceCountingCircles",
            columns: new[] { "CountingCircleId", "DomainOfInfluenceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircles_DomainOfInfluenceId",
            table: "DomainOfInfluenceCountingCircles",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircleSnapshots_BasisCountingCircl~",
            table: "DomainOfInfluenceCountingCircleSnapshots",
            column: "BasisCountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircleSnapshots_BasisDomainOfInflu~",
            table: "DomainOfInfluenceCountingCircleSnapshots",
            column: "BasisDomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircleSnapshots_BasisId",
            table: "DomainOfInfluenceCountingCircleSnapshots",
            column: "BasisId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircleSnapshots_ValidFrom",
            table: "DomainOfInfluenceCountingCircleSnapshots",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceHierarchies_DomainOfInfluenceId",
            table: "DomainOfInfluenceHierarchies",
            column: "DomainOfInfluenceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceParties_DomainOfInfluenceId",
            table: "DomainOfInfluenceParties",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluencePermissions_TenantId_DomainOfInfluenceId",
            table: "DomainOfInfluencePermissions",
            columns: new[] { "TenantId", "DomainOfInfluenceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluences_ParentId",
            table: "DomainOfInfluences",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceSnapshots_BasisId",
            table: "DomainOfInfluenceSnapshots",
            column: "BasisId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceSnapshots_ValidFrom",
            table: "DomainOfInfluenceSnapshots",
            column: "ValidFrom");

        migrationBuilder.CreateIndex(
            name: "IX_ElectionGroups_PrimaryMajorityElectionId",
            table: "ElectionGroups",
            column: "PrimaryMajorityElectionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EventLog_EventTenantId",
            table: "EventLog",
            column: "EventTenantId");

        migrationBuilder.CreateIndex(
            name: "IX_EventLog_EventUserId",
            table: "EventLog",
            column: "EventUserId");

        migrationBuilder.CreateIndex(
            name: "IX_EventLogTenant_TenantId",
            table: "EventLogTenant",
            column: "TenantId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EventLogUser_UserId",
            table: "EventLogUser",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExportConfigurations_DomainOfInfluenceId",
            table: "ExportConfigurations",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntries_BallotGroupId",
            table: "MajorityElectionBallotGroupEntries",
            column: "BallotGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntries_PrimaryMajorityElectionId",
            table: "MajorityElectionBallotGroupEntries",
            column: "PrimaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntries_SecondaryMajorityElectio~",
            table: "MajorityElectionBallotGroupEntries",
            column: "SecondaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntryCandidates_BallotGroupEntry~",
            table: "MajorityElectionBallotGroupEntryCandidates",
            column: "BallotGroupEntryId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntryCandidates_PrimaryElectionC~",
            table: "MajorityElectionBallotGroupEntryCandidates",
            column: "PrimaryElectionCandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntryCandidates_SecondaryElectio~",
            table: "MajorityElectionBallotGroupEntryCandidates",
            column: "SecondaryElectionCandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroups_MajorityElectionId",
            table: "MajorityElectionBallotGroups",
            column: "MajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidates_MajorityElectionId",
            table: "MajorityElectionCandidates",
            column: "MajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElections_ContestId",
            table: "MajorityElections",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElections_DomainOfInfluenceId",
            table: "MajorityElections",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionUnionEntries_MajorityElectionId_MajorityEle~",
            table: "MajorityElectionUnionEntries",
            columns: new[] { "MajorityElectionId", "MajorityElectionUnionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionUnionEntries_MajorityElectionUnionId",
            table: "MajorityElectionUnionEntries",
            column: "MajorityElectionUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionUnions_ContestId",
            table: "MajorityElectionUnions",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_PlausibilisationConfigurations_DomainOfInfluenceId",
            table: "PlausibilisationConfigurations",
            column: "DomainOfInfluenceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidates_PartyId",
            table: "ProportionalElectionCandidates",
            column: "PartyId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidates_ProportionalElectionListId",
            table: "ProportionalElectionCandidates",
            column: "ProportionalElectionListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionLists_ProportionalElectionId",
            table: "ProportionalElectionLists",
            column: "ProportionalElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnionEntries_ProportionalElectionLi~",
            table: "ProportionalElectionListUnionEntries",
            column: "ProportionalElectionListUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnions_ProportionalElectionId",
            table: "ProportionalElectionListUnions",
            column: "ProportionalElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnions_ProportionalElectionMainList~",
            table: "ProportionalElectionListUnions",
            column: "ProportionalElectionMainListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnions_ProportionalElectionRootList~",
            table: "ProportionalElectionListUnions",
            column: "ProportionalElectionRootListUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElections_ContestId",
            table: "ProportionalElections",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElections_DomainOfInfluenceId",
            table: "ProportionalElections",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionEntries_ProportionalElectionId_Pro~",
            table: "ProportionalElectionUnionEntries",
            columns: new[] { "ProportionalElectionId", "ProportionalElectionUnionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionEntries_ProportionalElectionUnionId",
            table: "ProportionalElectionUnionEntries",
            column: "ProportionalElectionUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionListEntries_ProportionalElectionLi~",
            table: "ProportionalElectionUnionListEntries",
            columns: new[] { "ProportionalElectionListId", "ProportionalElectionUnionListId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionListEntries_ProportionalElectionUn~",
            table: "ProportionalElectionUnionListEntries",
            column: "ProportionalElectionUnionListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionLists_ProportionalElectionUnionId",
            table: "ProportionalElectionUnionLists",
            column: "ProportionalElectionUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnions_ContestId",
            table: "ProportionalElectionUnions",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidates_CandidateReferenceId",
            table: "SecondaryMajorityElectionCandidates",
            column: "CandidateReferenceId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidates_SecondaryMajorityElecti~",
            table: "SecondaryMajorityElectionCandidates",
            column: "SecondaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElections_ElectionGroupId",
            table: "SecondaryMajorityElections",
            column: "ElectionGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElections_PrimaryMajorityElectionId",
            table: "SecondaryMajorityElections",
            column: "PrimaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_SimplePoliticalBusiness_ContestId",
            table: "SimplePoliticalBusiness",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_SimplePoliticalBusiness_DomainOfInfluenceId",
            table: "SimplePoliticalBusiness",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_TieBreakQuestions_BallotId_Question1Number_Question2Number",
            table: "TieBreakQuestions",
            columns: new[] { "BallotId", "Question1Number", "Question2Number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Votes_ContestId",
            table: "Votes",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_Votes_DomainOfInfluenceId",
            table: "Votes",
            column: "DomainOfInfluenceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Authorities");

        migrationBuilder.DropTable(
            name: "AuthoritySnapshots");

        migrationBuilder.DropTable(
            name: "BallotQuestions");

        migrationBuilder.DropTable(
            name: "CantonSettingsVotingCardChannels");

        migrationBuilder.DropTable(
            name: "ComparisonCountOfVotersConfigurations");

        migrationBuilder.DropTable(
            name: "ComparisonVoterParticipationConfigurations");

        migrationBuilder.DropTable(
            name: "ComparisonVotingChannelConfigurations");

        migrationBuilder.DropTable(
            name: "ContestCountingCircleOptions");

        migrationBuilder.DropTable(
            name: "CountingCircleContactPersons");

        migrationBuilder.DropTable(
            name: "CountingCircleContactPersonSnapshots");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceCountingCircles");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceCountingCircleSnapshots");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceHierarchies");

        migrationBuilder.DropTable(
            name: "DomainOfInfluencePermissions");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceSnapshots");

        migrationBuilder.DropTable(
            name: "EventLog");

        migrationBuilder.DropTable(
            name: "EventProcessingStates");

        migrationBuilder.DropTable(
            name: "ExportConfigurations");

        migrationBuilder.DropTable(
            name: "MajorityElectionBallotGroupEntryCandidates");

        migrationBuilder.DropTable(
            name: "MajorityElectionUnionEntries");

        migrationBuilder.DropTable(
            name: "PreconfiguredContestDates");

        migrationBuilder.DropTable(
            name: "ProportionalElectionCandidates");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListUnionEntries");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionEntries");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionListEntries");

        migrationBuilder.DropTable(
            name: "SimplePoliticalBusiness");

        migrationBuilder.DropTable(
            name: "TieBreakQuestions");

        migrationBuilder.DropTable(
            name: "CantonSettings");

        migrationBuilder.DropTable(
            name: "PlausibilisationConfigurations");

        migrationBuilder.DropTable(
            name: "CountingCircleSnapshots");

        migrationBuilder.DropTable(
            name: "CountingCircles");

        migrationBuilder.DropTable(
            name: "EventLogTenant");

        migrationBuilder.DropTable(
            name: "EventLogUser");

        migrationBuilder.DropTable(
            name: "MajorityElectionBallotGroupEntries");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropTable(
            name: "MajorityElectionUnions");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceParties");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListUnions");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionLists");

        migrationBuilder.DropTable(
            name: "Ballots");

        migrationBuilder.DropTable(
            name: "CountingCirclesMergers");

        migrationBuilder.DropTable(
            name: "MajorityElectionBallotGroups");

        migrationBuilder.DropTable(
            name: "MajorityElectionCandidates");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElections");

        migrationBuilder.DropTable(
            name: "ProportionalElectionLists");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnions");

        migrationBuilder.DropTable(
            name: "Votes");

        migrationBuilder.DropTable(
            name: "ElectionGroups");

        migrationBuilder.DropTable(
            name: "ProportionalElections");

        migrationBuilder.DropTable(
            name: "MajorityElections");

        migrationBuilder.DropTable(
            name: "Contests");

        migrationBuilder.DropTable(
            name: "DomainOfInfluences");
    }
}
