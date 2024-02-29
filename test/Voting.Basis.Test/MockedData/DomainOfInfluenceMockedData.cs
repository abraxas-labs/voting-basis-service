// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.EventProcessors;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Data.Repositories.Snapshot;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.MockedData;

public static class DomainOfInfluenceMockedData
{
    public const string IdBund = "eae2cfaf-c787-48b9-a108-c975b0a580da";
    public const string IdStGallen = "0e9ae002-dfa1-4eb6-920b-5d837a33ff79";
    public const string IdGossau = "a18b1c6a-6b66-43e4-a7d2-a440a7025dbd";
    public const string IdUzwil = "d9f5e161-37f8-4c76-921d-c0237c54dc5b";
    public const string IdKirchgemeinde = "22949a1e-fb09-47ec-9e9f-d26675676565";
    public const string IdKirchgemeindeAndere = "692be088-00e2-490c-b1fd-828adc38571d";
    public const string IdThurgau = "6bb6e372-ffa2-45fd-9329-6c6a0bc8a752";
    public const string IdGenf = "ade30989-5e28-40c7-918c-2c9408f853ba";
    public const string IdInvalid = "22949a1e";
    public const string IdNotExisting = "67674a69-4fdd-496a-bc83-a6585fc1419b";

    public const string ExportConfigurationIdBund001 = "a4079ec3-315c-4a41-b034-b86ff42b6063";
    public const string ExportConfigurationIdBund002 = "3bfb3250-f526-4f5e-9de7-6151f8f27d9e";

    public const string PartyIdBundAndere = "6fe8af5a-9f98-4457-90e7-49657a3df14f";
    public const string PartyIdStGallenSVP = "e6697ba9-9c7a-4dba-8640-9575dfcc3e09";
    public const string PartyIdStGallenSP = "d42d9fb1-4e68-4c4a-8d1f-7cf846c8304f";
    public const string PartyIdGossauFLiG = "171a2f0a-f037-4d73-8887-a31a3f5fb541";
    public const string PartyIdGossauDeleted = "4d7f6405-37be-4a6e-8317-e9c618da007c";
    public const string PartyIdKirchgemeindeEVP = "c4e19eb8-4bc3-4588-91b5-5acad046a0f6";

    public static readonly Guid GuidPartyBundAndere = Guid.Parse(PartyIdBundAndere);
    public static readonly Guid GuidPartyStGallenSVP = Guid.Parse(PartyIdStGallenSVP);
    public static readonly Guid GuidPartyStGallenSP = Guid.Parse(PartyIdStGallenSP);
    public static readonly Guid GuidPartyGossauFLiG = Guid.Parse(PartyIdGossauFLiG);
    public static readonly Guid GuidPartyGossauDeleted = Guid.Parse(PartyIdGossauDeleted);
    public static readonly Guid GuidPartyKirchgemeindeEVP = Guid.Parse(PartyIdKirchgemeindeEVP);

    public static readonly Guid GuidBund = Guid.Parse(IdBund);
    public static readonly Guid GuidStGallen = Guid.Parse(IdStGallen);
    public static readonly Guid GuidGossau = Guid.Parse(IdGossau);
    public static readonly Guid GuidUzwil = Guid.Parse(IdUzwil);
    public static readonly Guid GuidKirchgemeinde = Guid.Parse(IdKirchgemeinde);
    public static readonly Guid GuidKirchgemeindeAndere = Guid.Parse(IdKirchgemeindeAndere);
    public static readonly Guid GuidThurgau = Guid.Parse(IdThurgau);
    public static readonly Guid GuidGenf = Guid.Parse(IdGenf);

    public static DomainOfInfluence Bund
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdBund),
            Name = "Bund",
            ShortName = "Bund",
            SecureConnectId = "random",
            Type = DomainOfInfluenceType.Ch,
            Parent = null,
            Canton = DomainOfInfluenceCanton.Sg,
            ExportConfigurations = new List<ExportConfiguration>
            {
                    new ExportConfiguration
                    {
                        Id = Guid.Parse(ExportConfigurationIdBund001),
                        Description = "Intf001-BUND",
                        ExportKeys = new[]
                        {
                            AusmittlungXmlVoteTemplates.Ech0110.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                        },
                        EaiMessageType = "001",
                        Provider = ExportProvider.Standard,
                    },
                    new ExportConfiguration
                    {
                        Id = Guid.Parse(ExportConfigurationIdBund002),
                        Description = "Intf002-BUND",
                        ExportKeys = new[]
                        {
                            AusmittlungXmlVoteTemplates.Ech0222.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidatesNumerical.Key,
                        },
                        EaiMessageType = "002",
                        Provider = ExportProvider.Seantis,
                    },
            },
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(x =>
                x.ComparisonVoterParticipationConfigurations = new List<ComparisonVoterParticipationConfiguration>
                {
                        new()
                        {
                            MainLevel = DomainOfInfluenceType.Ch,
                            ComparisonLevel = DomainOfInfluenceType.Ch,
                            ThresholdPercent = 1.0M,
                        },
                }),
            Parties = new List<DomainOfInfluenceParty>
            {
                    PartyBundAndere,
            },
        };

    public static DomainOfInfluence StGallen
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdStGallen),
            Name = "St. Gallen",
            NameForProtocol = "Kanton St. Gallen",
            ShortName = "SG",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            Type = DomainOfInfluenceType.Ct,
            ParentId = Guid.Parse(IdBund),
            ContactPerson = new ContactPerson
            {
                Email = "hans@muster.com",
                Phone = "071 123 12 12",
                FamilyName = "Muster",
                FirstName = "Hans",
                MobilePhone = "079 123 12 12",
            },
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(x =>
                x.ComparisonVoterParticipationConfigurations = new List<ComparisonVoterParticipationConfiguration>
                {
                        new()
                        {
                            MainLevel = DomainOfInfluenceType.Ch,
                            ComparisonLevel = DomainOfInfluenceType.Ch,
                            ThresholdPercent = 1.0M,
                        },
                        new()
                        {
                            MainLevel = DomainOfInfluenceType.Ct,
                            ComparisonLevel = DomainOfInfluenceType.Ch,
                            ThresholdPercent = 2M,
                        },
                        new()
                        {
                            MainLevel = DomainOfInfluenceType.Ct,
                            ComparisonLevel = DomainOfInfluenceType.Ct,
                            ThresholdPercent = 2.5M,
                        },
                }),
            Parties = new List<DomainOfInfluenceParty>
            {
                    PartyStGallenSP,
                    PartyStGallenSVP,
            },
        };

    public static DomainOfInfluence Uzwil
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdUzwil),
            Name = "Uzwil",
            NameForProtocol = "Stadt Uzwil",
            ShortName = "UZW",
            Bfs = "3408",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = DomainOfInfluenceType.Sk,
            ParentId = Guid.Parse(IdStGallen),
            ExportConfigurations = new List<ExportConfiguration>
            {
                    new ExportConfiguration
                    {
                        Id = Guid.Parse("65d15f77-03c5-4061-9605-873c0d89cb3a"),
                        Description = "Intf001",
                        ExportKeys = new[]
                        {
                            AusmittlungXmlVoteTemplates.Ech0110.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                        },
                        EaiMessageType = "001",
                        Provider = ExportProvider.Standard,
                    },
                    new ExportConfiguration
                    {
                        Id = Guid.Parse("4f644c92-65c1-4c6b-8a94-ea5f9d08ecc3"),
                        Description = "Intf002",
                        ExportKeys = new[]
                        {
                            AusmittlungXmlVoteTemplates.Ech0222.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidatesNumerical.Key,
                        },
                        EaiMessageType = "002",
                        Provider = ExportProvider.Seantis,
                    },
            },
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(),
        };

    public static DomainOfInfluence Gossau
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdGossau),
            Name = "Gossau",
            NameForProtocol = "Stadt Gossau",
            ShortName = "GOS",
            Bfs = "3443",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            Type = DomainOfInfluenceType.Sk,
            ParentId = Guid.Parse(IdStGallen),
            ExportConfigurations = new List<ExportConfiguration>
            {
                    new ExportConfiguration
                    {
                        Id = Guid.Parse("f40aad9d-771e-46ab-ba60-5554a7466074"),
                        Description = "Intf001-GO",
                        ExportKeys = new[]
                        {
                            AusmittlungXmlVoteTemplates.Ech0110.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                        },
                        EaiMessageType = "001",
                        Provider = ExportProvider.Seantis,
                    },
                    new ExportConfiguration
                    {
                        Id = Guid.Parse("c7dd84f9-bec0-4cea-9584-f7ab2d7768e3"),
                        Description = "Intf002-GO",
                        ExportKeys = new[]
                        {
                            AusmittlungXmlVoteTemplates.Ech0222.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidatesNumerical.Key,
                        },
                        EaiMessageType = "002",
                        Provider = ExportProvider.Standard,
                    },
            },
            ResponsibleForVotingCards = true,
            PrintData = new DomainOfInfluenceVotingCardPrintData
            {
                ShippingAway = VotingCardShippingFranking.B2,
                ShippingReturn = VotingCardShippingFranking.GasB,
                ShippingMethod = VotingCardShippingMethod.OnlyPrintingPackagingToMunicipality,
                ShippingVotingCardsToDeliveryAddress = true,
            },
            ReturnAddress = new DomainOfInfluenceVotingCardReturnAddress
            {
                AddressLine1 = "Stadtverwaltung Gossau",
                Street = "Bahnhofstrasse 25",
                ZipCode = "9200",
                City = "Gossau",
                Country = "Schweiz",
            },
            ExternalPrintingCenter = true,
            ExternalPrintingCenterEaiMessageType = "EAI-Gossau",
            SapCustomerOrderNumber = "0005400492",
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(),
            Parties = new List<DomainOfInfluenceParty>
            {
                    PartyGossauFLiG,
                    PartyGossauDeleted,
            },
            SwissPostData = new DomainOfInfluenceVotingCardSwissPostData
            {
                InvoiceReferenceNumber = "958473825",
                FrankingLicenceReturnNumber = "562984257",
            },
        };

    public static DomainOfInfluence Kirchgemeinde
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdKirchgemeinde),
            Name = "Kirchgemeinde Oberuzwil",
            ShortName = "KOBZ",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = DomainOfInfluenceType.Ki,
            ParentId = null,
            Canton = DomainOfInfluenceCanton.Sg,
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(),
            Parties = new List<DomainOfInfluenceParty>
            {
                    PartyKirchgemeindeEVP,
            },
        };

    public static DomainOfInfluence KirchgemeindeAndere
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdKirchgemeindeAndere),
            Name = "Oberuzwil Kirche andere",
            NameForProtocol = "Oberuzwil Kirche andere",
            ShortName = "OUKA",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = DomainOfInfluenceType.An,
            ParentId = Guid.Parse(IdKirchgemeinde),
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(),
        };

    public static DomainOfInfluence Thurgau
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdThurgau),
            Name = "Thurgau",
            NameForProtocol = "Kanton Thurgau",
            ShortName = "TG",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            Type = DomainOfInfluenceType.Ct,
            ParentId = Guid.Parse(IdBund),
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(),
        };

    public static DomainOfInfluence Genf
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdGenf),
            Name = "Genf",
            NameForProtocol = "Kanton Genf",
            ShortName = "Genf",
            SecureConnectId = "random",
            Type = DomainOfInfluenceType.Ct,
            ParentId = Guid.Parse(IdBund),
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(),
        };

    public static IEnumerable<DomainOfInfluence> All
    {
        get
        {
            yield return Bund;
            yield return StGallen;
            yield return Gossau;
            yield return Uzwil;
            yield return Kirchgemeinde;
            yield return Thurgau;
            yield return Genf;
            yield return KirchgemeindeAndere;
        }
    }

    public static DomainOfInfluenceParty PartyBundAndere
        => new DomainOfInfluenceParty
        {
            Id = Guid.Parse(PartyIdBundAndere),
            Name = LanguageUtil.MockAllLanguages("Andere"),
            ShortDescription = LanguageUtil.MockAllLanguages("AN"),
        };

    public static DomainOfInfluenceParty PartyStGallenSP
        => new DomainOfInfluenceParty
        {
            Id = Guid.Parse(PartyIdStGallenSP),
            Name = LanguageUtil.MockAllLanguages("Sozialdemokratische Partei"),
            ShortDescription = LanguageUtil.MockAllLanguages("SP"),
        };

    public static DomainOfInfluenceParty PartyStGallenSVP
        => new DomainOfInfluenceParty
        {
            Id = Guid.Parse(PartyIdStGallenSVP),
            Name = LanguageUtil.MockAllLanguages("Schweizerische Volkspartei"),
            ShortDescription = LanguageUtil.MockAllLanguages("SVP"),
        };

    public static DomainOfInfluenceParty PartyGossauFLiG
        => new DomainOfInfluenceParty
        {
            Id = Guid.Parse(PartyIdGossauFLiG),
            Name = LanguageUtil.MockAllLanguages("Freie Liste Gossau"),
            ShortDescription = LanguageUtil.MockAllLanguages("FLiG"),
        };

    public static DomainOfInfluenceParty PartyGossauDeleted
        => new DomainOfInfluenceParty
        {
            Id = Guid.Parse(PartyIdGossauDeleted),
            Name = LanguageUtil.MockAllLanguages("Deleted"),
            ShortDescription = LanguageUtil.MockAllLanguages("DEL"),
            Deleted = true,
        };

    public static DomainOfInfluenceParty PartyKirchgemeindeEVP
        => new DomainOfInfluenceParty
        {
            Id = Guid.Parse(PartyIdKirchgemeindeEVP),
            Name = LanguageUtil.MockAllLanguages("Evangelische Volks Partei"),
            ShortDescription = LanguageUtil.MockAllLanguages("EVP"),
        };

    public static IEnumerable<DomainOfInfluenceCountingCircle> GetAllDomainOfInfluenceCountingCircles()
    {
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("ab32b5c3-48db-4242-90b6-16a13e94f8d9"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdBund),
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("b8d29bd5-a828-4783-807a-a3813e24144c"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdStGallen),
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("22c686ca-b3ee-4be6-af94-20ce03784416"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdRapperswil),
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("52e1ae3b-23af-4536-996d-41cc5cedb445"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdGossau),
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("085cdabd-c2d3-4d9e-b558-58706026d374"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwil),
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("dbf7dea8-3739-4e15-9e67-37f2f2fd6f1f"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdStGallen),
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("b6bd656e-c2d8-4912-bab9-dfdbdbaacd0f"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwil),
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("bfb20b67-93e6-4da5-8cd3-1d679333631f"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdRapperswil),
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("260ca8b9-3967-4d76-b382-e3349b215055"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdGossau),
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("ae8b1e0f-7ba0-4ec6-a25a-7acbcb42778f"),
            DomainOfInfluenceId = Guid.Parse(IdGossau),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdGossau),
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("458a7b20-3eb3-42ba-bc9c-adeed71527d6"),
            DomainOfInfluenceId = Guid.Parse(IdUzwil),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwil),
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("a23b7b20-3eb3-42ba-bc9c-adeed71555c3"),
            DomainOfInfluenceId = Guid.Parse(IdKirchgemeinde),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwilKirche),
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("e5c3b361-cfdb-41bc-8f13-c057c58d857b"),
            DomainOfInfluenceId = Guid.Parse(IdKirchgemeinde),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwilKircheAndere),
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("c8ba7cb5-9155-4f1a-a232-3b44f014cf9d"),
            DomainOfInfluenceId = Guid.Parse(IdKirchgemeindeAndere),
            CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwilKircheAndere),
        };
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await CountingCircleMockedData.Seed(runScoped);

        await runScoped(async sp =>
        {
            var doiRepo = sp.GetRequiredService<HasSnapshotDbRepository<DomainOfInfluence, DomainOfInfluenceSnapshot>>();
            var doiCcRepo = sp.GetRequiredService<DomainOfInfluenceCountingCircleRepo>();

            foreach (var doi in All)
            {
                await doiRepo.Create(doi, new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            }

            await doiCcRepo.AddRange(GetAllDomainOfInfluenceCountingCircles().ToList(), new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var permissionBuilder = sp.GetRequiredService<DomainOfInfluencePermissionBuilder>();
            await permissionBuilder.RebuildPermissionTree();
            var hierarchyBuilder = sp.GetRequiredService<DomainOfInfluenceHierarchyBuilder>();
            await hierarchyBuilder.RebuildHierarchy();

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            var domainOfInfluenceAggregates = All.Select(doi => ToAggregate(doi, aggregateFactory, mapper));

            foreach (var domainOfInfluenceAggregate in domainOfInfluenceAggregates)
            {
                await aggregateRepository.Save(domainOfInfluenceAggregate);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });

        await CantonSettingsMockedData.Seed(runScoped);
    }

    public static DomainOfInfluenceAggregate ToAggregate(
        DomainOfInfluence domainOfInfluence,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<DomainOfInfluenceAggregate>();
        domainOfInfluence.CreatedOn = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        domainOfInfluence.ModifiedOn = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var doi = mapper.Map<Core.Domain.DomainOfInfluence>(domainOfInfluence);
        aggregate.CreateFrom(doi);
        return aggregate;
    }

    public static ProtoModels.PlausibilisationConfiguration BuildPlausibilisationConfiguration(Action<ProtoModels.PlausibilisationConfiguration>? customizer = null)
    {
        var plausiConfig = new ProtoModels.PlausibilisationConfiguration
        {
            ComparisonCountOfVotersConfigurations =
                {
                    new ProtoModels.ComparisonCountOfVotersConfiguration { Category = SharedProto.ComparisonCountOfVotersCategory.A, ThresholdPercent = 4 },
                    new ProtoModels.ComparisonCountOfVotersConfiguration { Category = SharedProto.ComparisonCountOfVotersCategory.B, ThresholdPercent = null },
                    new ProtoModels.ComparisonCountOfVotersConfiguration { Category = SharedProto.ComparisonCountOfVotersCategory.C, ThresholdPercent = 1.25 },
                },
            ComparisonVotingChannelConfigurations =
                {
                    new ProtoModels.ComparisonVotingChannelConfiguration { VotingChannel = SharedProto.VotingChannel.BallotBox, ThresholdPercent = 4 },
                    new ProtoModels.ComparisonVotingChannelConfiguration { VotingChannel = SharedProto.VotingChannel.ByMail, ThresholdPercent = null },
                    new ProtoModels.ComparisonVotingChannelConfiguration { VotingChannel = SharedProto.VotingChannel.EVoting, ThresholdPercent = 1.25 },
                    new ProtoModels.ComparisonVotingChannelConfiguration { VotingChannel = SharedProto.VotingChannel.Paper, ThresholdPercent = 7.5 },
                },
            ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 1.25,
        };

        customizer?.Invoke(plausiConfig);
        return plausiConfig;
    }

    private static PlausibilisationConfiguration BuildDataPlausibilisationConfiguration(Action<PlausibilisationConfiguration>? customizer = null)
    {
        var plausiConfig = new PlausibilisationConfiguration
        {
            ComparisonCountOfVotersConfigurations = new List<ComparisonCountOfVotersConfiguration>()
                {
                    new() { Category = ComparisonCountOfVotersCategory.A, ThresholdPercent = 6.5M },
                    new() { Category = ComparisonCountOfVotersCategory.B, ThresholdPercent = 6.4M },
                    new() { Category = ComparisonCountOfVotersCategory.C, ThresholdPercent = null },
                },
            ComparisonVotingChannelConfigurations = new List<ComparisonVotingChannelConfiguration>()
                {
                    new() { VotingChannel = VotingChannel.BallotBox, ThresholdPercent = 2M },
                    new() { VotingChannel = VotingChannel.ByMail, ThresholdPercent = 7.5M },
                    new() { VotingChannel = VotingChannel.EVoting, ThresholdPercent = null },
                    new() { VotingChannel = VotingChannel.Paper, ThresholdPercent = 7.5M },
                },
            ComparisonVoterParticipationConfigurations = new List<ComparisonVoterParticipationConfiguration>(),
            ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 1.25M,
        };
        customizer?.Invoke(plausiConfig);
        return plausiConfig;
    }
}
