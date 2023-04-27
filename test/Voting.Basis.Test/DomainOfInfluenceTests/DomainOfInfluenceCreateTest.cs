﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceCreateTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldPublishAndReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(x =>
        {
            x.PlausibilisationConfiguration.ComparisonVoterParticipationConfigurations.Add(new ProtoModels.ComparisonVoterParticipationConfiguration
            {
                MainLevel = SharedProto.DomainOfInfluenceType.Ct,
                ComparisonLevel = SharedProto.DomainOfInfluenceType.Ch,
                ThresholdPercent = 2.0,
            });
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();
        var exportConfigCreated = EventPublisherMock.GetSinglePublishedEvent<ExportConfigurationCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.DomainOfInfluence.Id.Should().Be(exportConfigCreated.Configuration.DomainOfInfluenceId);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);

        exportConfigCreated.Configuration.Id = string.Empty;
        exportConfigCreated.Configuration.DomainOfInfluenceId = string.Empty;
        exportConfigCreated.MatchSnapshot("exportConfigCreated");
    }

    [Fact]
    public async Task TestRootDoiShouldReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRootDoiRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task TestChildDoiAggregateShouldSaveToDatabase()
    {
        var newId = Guid.Parse("3c3f3ae2-0439-4998-85ff-ae1f7eac94a3");

        await TestEventPublisher.Publish(new DomainOfInfluenceCreated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = newId.ToString(),
                Name = "Bezirk Uzwil",
                ShortName = "BZ Uz",
                Bfs = "3442",
                Code = "C3442",
                SortNumber = 53,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                ParentId = DomainOfInfluenceMockedData.IdUzwil,
                Type = SharedProto.DomainOfInfluenceType.Bz,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var domainOfInfluences = await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        domainOfInfluences.MatchSnapshot("domainOfInfluences");

        var dbData = await RunOnDb(async db => await db.FindAsync<DomainOfInfluence>(newId));
        dbData!.CantonDefaults.MatchSnapshot("cantonDefaults");
    }

    [Fact]
    public async Task TestRootDoiAggregateShouldSaveToDatabase()
    {
        var newId = Guid.Parse("3c3f3ae2-0439-4998-85ff-ae1f7eac94a3");

        await TestEventPublisher.Publish(new DomainOfInfluenceCreated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = newId.ToString(),
                Name = "Bund",
                NameForProtocol = "Bund",
                ShortName = "Bund",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                ParentId = string.Empty,
                Type = SharedProto.DomainOfInfluenceType.Ch,
                Canton = SharedProto.DomainOfInfluenceCanton.Zh,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var domainOfInfluences = await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        domainOfInfluences.MatchSnapshot("domainOfInfluences");

        var dbData = await RunOnDb(async db => await db.FindAsync<DomainOfInfluence>(newId));
        dbData!.CantonDefaults.MatchSnapshot("cantonDefaults");
    }

    [Fact]
    public async Task ShouldCreateWithParties()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(x =>
        {
            x.Parties.Add(new ProtoModels.DomainOfInfluenceParty
            {
                Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
            });
            x.Parties.Add(new ProtoModels.DomainOfInfluenceParty
            {
                Name = { LanguageUtil.MockAllLanguages("Neue Partei2") },
                ShortDescription = { LanguageUtil.MockAllLanguages("NP2") },
            });
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();
        var partiesCreated = EventPublisherMock.GetPublishedEvents<DomainOfInfluencePartyCreated>();

        foreach (var partyCreated in partiesCreated)
        {
            partyCreated.Party.Id = string.Empty;
            partyCreated.Party.DomainOfInfluenceId = string.Empty;
        }

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);

        partiesCreated.MatchSnapshot("partiesCreated");
    }

    [Fact]
    public async Task NonPoliticalShouldPublishAndReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRootDoiRequest(x => x.Type = SharedProto.DomainOfInfluenceType.Ki));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task ResponsibleForVotingCardsShouldReturn()
    {
        var response = await AdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest());
        var votingCardDataUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceVotingCardDataUpdated>();

        votingCardDataUpdated.DomainOfInfluenceId.Should().Be(response.Id);
        votingCardDataUpdated.MatchSnapshot("votingCardDataUpdated", d => d.DomainOfInfluenceId);
    }

    [Fact]
    public async Task InvalidParentShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.ParentId = DomainOfInfluenceMockedData.IdNotExisting)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ParentPoliticalSelfNonPoliticalShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.ParentId = DomainOfInfluenceMockedData.IdBund;
                o.Type = SharedProto.DomainOfInfluenceType.Ki;
            })),
            StatusCode.InvalidArgument,
            "Non political DomainOfInfluence may only be a child of a non political DomainOfInfluence Parent");
    }

    [Fact]
    public async Task ParentNonPoliticalSelfPoliticalShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.ParentId = DomainOfInfluenceMockedData.IdKirchgemeinde;
                o.Type = SharedProto.DomainOfInfluenceType.Ct;
            })),
            StatusCode.InvalidArgument,
            "Political DomainOfInfluence may only be a child of a political DomainOfInfluence Parent");
    }

    [Fact]
    public async Task ParentNonPoliticalSelfNonPoliticalShouldReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(o =>
        {
            o.ParentId = DomainOfInfluenceMockedData.IdKirchgemeinde;
            o.Type = SharedProto.DomainOfInfluenceType.An;
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task ParentPoliticalTypeEqualSelfShouldReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(o =>
        {
            o.ParentId = DomainOfInfluenceMockedData.IdBund;
            o.Type = SharedProto.DomainOfInfluenceType.Ch;
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task ParentNonPoliticalTypeEqualSelfShouldReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(o =>
        {
            o.ParentId = DomainOfInfluenceMockedData.IdKirchgemeinde;
            o.Type = SharedProto.DomainOfInfluenceType.Ki;
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task ParentTypeSmallerThanSelfShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o => o.ParentId = DomainOfInfluenceMockedData.IdUzwil)),
            StatusCode.InvalidArgument,
            "political hierarchical order");
    }

    [Fact]
    public async Task ParentTypeLargerThanSelfShouldReturnOk()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest(o => o.ParentId = DomainOfInfluenceMockedData.IdBund));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task NoCantonOnRootShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRootDoiRequest(o => o.Canton = SharedProto.DomainOfInfluenceCanton.Unspecified)),
            StatusCode.InvalidArgument,
            "Canton");
    }

    [Fact]
    public async Task NoBfsForMunicipalityShouldThrow()
    {
        var req = NewValidRequest(o =>
        {
            o.Type = SharedProto.DomainOfInfluenceType.Mu;
            o.Bfs = string.Empty;
        });

        await AssertStatus(
            async () => await AdminClient.CreateAsync(req),
            StatusCode.InvalidArgument,
            "Bfs");
    }

    [Fact]
    public async Task DuplicatedBfsForMunicipalityShouldThrow()
    {
        var req = NewValidRequest(o =>
        {
            o.Type = SharedProto.DomainOfInfluenceType.Mu;
            o.Bfs = DomainOfInfluenceMockedData.Uzwil.Bfs;
        });

        await AssertStatus(
            async () => await AdminClient.CreateAsync(req),
            StatusCode.AlreadyExists,
            "The bfs 3408 is already taken");
    }

    [Fact]
    public Task ShouldThrowNotResponsibleForVotingCardsWithVotingCardData()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ResponsibleForVotingCards = false)),
            StatusCode.InvalidArgument);

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoReturnAddress()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ReturnAddress = null)),
            StatusCode.InvalidArgument,
            "ReturnAddress");

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoPrintData()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData = null)),
            StatusCode.InvalidArgument,
            "PrintData");

    [Fact]
    public Task ShouldThrowNoExternalPrintingCenterEaiMessageTypeWithExternalPrintingCenterSet()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ExternalPrintingCenterEaiMessageType = string.Empty)),
            StatusCode.InvalidArgument,
            "ExternalPrintingCenterEaiMessageType");

    [Fact]
    public Task ShouldThrowComparisonVoterParticipationConfigurationDuplicate()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(
                NewValidRequest(o =>
                {
                    o.PlausibilisationConfiguration.ComparisonVoterParticipationConfigurations.Add(new ProtoModels.ComparisonVoterParticipationConfiguration
                    {
                        MainLevel = SharedProto.DomainOfInfluenceType.Ch,
                        ComparisonLevel = SharedProto.DomainOfInfluenceType.Ct,
                        ThresholdPercent = 1.0,
                    });
                    o.PlausibilisationConfiguration.ComparisonVoterParticipationConfigurations.Add(new ProtoModels.ComparisonVoterParticipationConfiguration
                    {
                        MainLevel = SharedProto.DomainOfInfluenceType.Ch,
                        ComparisonLevel = SharedProto.DomainOfInfluenceType.Ct,
                        ThresholdPercent = 1.8,
                    });
                })),
            StatusCode.InvalidArgument,
            "ComparisonVoterParticipationConfigurations");

    [Fact]
    public Task ShouldThrowComparisonCountOfVotersConfigurationMissing()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowComparisonCountOfVotersConfigurationDuplicate()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations[0].Category = SharedProto.ComparisonCountOfVotersCategory.C)),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowAnyComparisonCountOfVotersCc()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersCountingCircleEntries.Add(new ProtoModels.ComparisonCountOfVotersCountingCircleEntry
                {
                    Category = SharedProto.ComparisonCountOfVotersCategory.B,
                    CountingCircleId = CountingCircleMockedData.IdBund,
                }))),
            StatusCode.InvalidArgument,
            "only allowed on update");

    [Fact]
    public Task ShouldThrowComparisonVotingChannelConfigurationMissing()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonVotingChannelConfigurations");

    [Fact]
    public Task ShouldThrowComparisonVotingChannelConfigurationDuplicate()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations[0].VotingChannel = SharedProto.VotingChannel.EVoting)),
            StatusCode.InvalidArgument,
            "ComparisonVotingChannelConfigurations");

    [Theory]
    [InlineData(SharedProto.VotingCardShippingFranking.Unspecified)]
    [InlineData(SharedProto.VotingCardShippingFranking.GasA)]
    [InlineData(SharedProto.VotingCardShippingFranking.GasB)]
    [InlineData(SharedProto.VotingCardShippingFranking.WithoutFranking)]
    public Task ShouldThrowResponsibleForVotingCardsInvalidShippingAway(SharedProto.VotingCardShippingFranking franking)
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingAway = franking)),
            StatusCode.InvalidArgument,
            "ShippingAway");

    [Theory]
    [InlineData(SharedProto.VotingCardShippingFranking.Unspecified)]
    [InlineData(SharedProto.VotingCardShippingFranking.B1)]
    [InlineData(SharedProto.VotingCardShippingFranking.B2)]
    [InlineData(SharedProto.VotingCardShippingFranking.A)]
    public Task ShouldThrowResponsibleForVotingCardsInvalidShippingReturn(SharedProto.VotingCardShippingFranking franking)
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingReturn = franking)),
            StatusCode.InvalidArgument,
            "ShippingReturn");

    [Fact]
    public async Task ShouldThrowForeignParty()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.Parties.Add(new ProtoModels.DomainOfInfluenceParty
                {
                    Id = DomainOfInfluenceMockedData.PartyIdKirchgemeindeEVP,
                    Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
                });
            })),
            StatusCode.InvalidArgument,
            "Some parties cannot be modified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .CreateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }

    private CreateDomainOfInfluenceRequest NewValidRootDoiRequest(
        Action<CreateDomainOfInfluenceRequest>? customizer = null)
    {
        var request = new CreateDomainOfInfluenceRequest
        {
            Name = "Bezirk Uzwil",
            ShortName = "BZ Uz",
            AuthorityName = "Gemeinde Uzwil",
            Bfs = "2442",
            Code = "C2355",
            SortNumber = 3,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = SharedProto.DomainOfInfluenceType.Bz,
            Canton = SharedProto.DomainOfInfluenceCanton.Sg,
            ContactPerson = new ProtoModels.ContactPerson
            {
                Email = "hans@muster.com",
                Phone = "071 123 12 12",
                FamilyName = "muster",
                FirstName = "hans",
                MobilePhone = "079 721 21 21",
            },
            PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(),
        };
        customizer?.Invoke(request);
        return request;
    }

    private CreateDomainOfInfluenceRequest NewValidRequest(Action<CreateDomainOfInfluenceRequest>? customizer = null)
    {
        var request = new CreateDomainOfInfluenceRequest
        {
            Name = "Bezirk Uzwil",
            NameForProtocol = "Uzwil",
            ShortName = "BZ Uz",
            AuthorityName = "Gemeinde Uzwil",
            Bfs = "3408",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = SharedProto.DomainOfInfluenceType.Bz,
            ParentId = DomainOfInfluenceMockedData.IdStGallen,
            ResponsibleForVotingCards = false,
            ContactPerson = new ProtoModels.ContactPerson
            {
                Email = "hans@muster.com",
                Phone = "071 123 12 12",
                FamilyName = "muster",
                FirstName = "hans",
                MobilePhone = "079 721 21 21",
            },
            ExportConfigurations =
                {
                    new ProtoModels.ExportConfiguration
                    {
                        Description = "Intf001",
                        ExportKeys =
                        {
                            AusmittlungXmlVoteTemplates.Ech0110.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                        },
                        EaiMessageType = "1234657",
                        Provider = SharedProto.ExportProvider.Seantis,
                    },
                },
            PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(x =>
            {
                x.ComparisonCountOfVotersConfigurations[0].ThresholdPercent = 9.91;
                x.ComparisonVotingChannelConfigurations[0].ThresholdPercent = 0.15;
            }),
        };
        customizer?.Invoke(request);
        return request;
    }

    private CreateDomainOfInfluenceRequest NewValidResponsibleForVotingCardsRequest(Action<CreateDomainOfInfluenceRequest>? customizer = null)
    {
        var request = new CreateDomainOfInfluenceRequest
        {
            Name = "Bezirk Uzwil",
            ShortName = "BZ Uz",
            AuthorityName = "Gemeinde Uzwil",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = SharedProto.DomainOfInfluenceType.Bz,
            ParentId = DomainOfInfluenceMockedData.IdStGallen,
            Canton = SharedProto.DomainOfInfluenceCanton.Sg, // Should be ignored unless root
            ResponsibleForVotingCards = true,
            ExternalPrintingCenter = true,
            ExternalPrintingCenterEaiMessageType = "1234567",
            ReturnAddress = new ProtoModels.DomainOfInfluenceVotingCardReturnAddress
            {
                AddressLine1 = "Zeile 1",
                AddressLine2 = "Zeile 2",
                AddressAddition = "Addition",
                City = "City",
                Country = "Schweiz",
                Street = "Street",
                ZipCode = "1000",
            },
            PrintData = new ProtoModels.DomainOfInfluenceVotingCardPrintData
            {
                ShippingAway = SharedProto.VotingCardShippingFranking.B1,
                ShippingReturn = SharedProto.VotingCardShippingFranking.GasA,
                ShippingMethod = SharedProto.VotingCardShippingMethod.PrintingPackagingShippingToCitizen,
            },
            ContactPerson = new ProtoModels.ContactPerson
            {
                Email = "hans@muster.com",
                Phone = "071 123 12 12",
                FamilyName = "muster",
                FirstName = "hans",
                MobilePhone = "079 721 21 21",
            },
            ExportConfigurations =
                {
                    new ProtoModels.ExportConfiguration
                    {
                        Description = "Intf001",
                        ExportKeys =
                        {
                            AusmittlungXmlVoteTemplates.Ech0110.Key,
                            AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                        },
                        EaiMessageType = "1234567",
                        Provider = SharedProto.ExportProvider.Standard,
                    },
                },
            PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(),
        };
        customizer?.Invoke(request);
        return request;
    }
}
