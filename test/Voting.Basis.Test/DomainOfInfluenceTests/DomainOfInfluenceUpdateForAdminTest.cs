// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateForAdminTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceUpdateForAdminTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldPublishAndReturn()
    {
        await AdminClient.UpdateAsync(NewValidRequest(x =>
        {
            x.PlausibilisationConfiguration.ComparisonVoterParticipationConfigurations.Add(new ProtoModels.ComparisonVoterParticipationConfiguration
            {
                MainLevel = SharedProto.DomainOfInfluenceType.Ct,
                ComparisonLevel = SharedProto.DomainOfInfluenceType.Ch,
                ThresholdPercent = 2.0,
            });
            x.Parties.Add(new ProtoModels.DomainOfInfluenceParty
            {
                Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
            });
        }));

        var domainOfInfluenceUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceUpdated>();
        domainOfInfluenceUpdated.MatchSnapshot("domainOfInfluenceUpdated", d => d.DomainOfInfluence.Id);

        var plausiConfigUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePlausibilisationConfigurationUpdated>();
        plausiConfigUpdated.MatchSnapshot("plausiConfigUpdated");

        var contactPersonUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceContactPersonUpdated>();
        contactPersonUpdated.MatchSnapshot("contactPersonUpdated");
    }

    [Fact]
    public async Task TestRootDoiShouldReturn()
    {
        await AdminClient.UpdateAsync(NewValidRootDoiRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceUpdated>();
        var exportConfigCreated = EventPublisherMock.GetSinglePublishedEvent<ExportConfigurationCreated>();
        var exportConfigUpdated = EventPublisherMock.GetSinglePublishedEvent<ExportConfigurationUpdated>();
        var exportConfigDeleted = EventPublisherMock.GetSinglePublishedEvent<ExportConfigurationDeleted>();

        eventData.DomainOfInfluence.Id.Should().Be(exportConfigCreated.Configuration.DomainOfInfluenceId);
        eventData.DomainOfInfluence.Id.Should().Be(exportConfigUpdated.Configuration.DomainOfInfluenceId);
        eventData.DomainOfInfluence.Id.Should().Be(exportConfigDeleted.DomainOfInfluenceId);
        exportConfigDeleted.ConfigurationId.Should().Be(DomainOfInfluenceMockedData.ExportConfigurationIdBund002);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);

        exportConfigCreated.Configuration.Id = string.Empty;
        exportConfigCreated.MatchSnapshot("exportConfigCreated");
        exportConfigUpdated.MatchSnapshot("exportConfigUpdated");
    }

    [Fact]
    public async Task TestChildDoiAggregateShouldUpdateDatabase()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceUpdated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
                Name = "St. Gallen Neu",
                ShortName = "Sankt1",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                ParentId = DomainOfInfluenceMockedData.IdBund,
                Type = SharedProto.DomainOfInfluenceType.Ct,
            },
            EventInfo = GetMockedEventInfo(),
        });

        await TestEventPublisher.Publish(1, new DomainOfInfluenceUpdated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
                Name = "St. Gallen Neu2",
                ShortName = "Sankt1",
                Bfs = "442",
                Code = "C442",
                SortNumber = 2,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                ParentId = DomainOfInfluenceMockedData.IdBund,
                Type = SharedProto.DomainOfInfluenceType.Ct,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var saved = await RunOnDb(async db => await db.FindAsync<DomainOfInfluence>(DomainOfInfluenceMockedData.GuidStGallen));
        saved.MatchSnapshot("db");

        var domainOfInfluences = await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        domainOfInfluences.MatchSnapshot("list");
    }

    [Fact]
    public async Task TestRootDoiAggregateShouldUpdateDatabase()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceUpdated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = DomainOfInfluenceMockedData.IdBund,
                Name = "Bund Neu",
                ShortName = "Bund Neu",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                Type = SharedProto.DomainOfInfluenceType.Ch,
                Canton = SharedProto.DomainOfInfluenceCanton.Tg,
            },
            EventInfo = GetMockedEventInfo(),
        });

        await TestEventPublisher.Publish(1, new DomainOfInfluenceUpdated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
                Name = "St. Gallen Neu2",
                NameForProtocol = "Kanton St. Gallen",
                ShortName = "Sankt1",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                ParentId = DomainOfInfluenceMockedData.IdBund,
                Type = SharedProto.DomainOfInfluenceType.Ct,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var saved = await RunOnDb(async db => await db.FindAsync<DomainOfInfluence>(DomainOfInfluenceMockedData.GuidStGallen));
        saved.MatchSnapshot("db");

        var domainOfInfluences = await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        domainOfInfluences.MatchSnapshot("list");
    }

    [Fact]
    public async Task TestRootDoiAggregateCantonChangeShouldUpdateChildDefaults()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceUpdated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = DomainOfInfluenceMockedData.IdBund,
                Name = "Bund Neu",
                ShortName = "Bund Neu",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                Type = SharedProto.DomainOfInfluenceType.Ch,
                Canton = SharedProto.DomainOfInfluenceCanton.Zh,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var affectedDoiIds = (await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest()))
            .DomainOfInfluences_
            .Where(doi => doi.Id == DomainOfInfluenceMockedData.IdBund)
            .ToList()
            .Flatten(doi => doi.Children)
            .Select(doi => Guid.Parse(doi.Id))
            .ToList();

        var affectedDois = await RunOnDb(db => db.DomainOfInfluences
            .Where(doi => affectedDoiIds.Contains(doi.Id))
            .OrderBy(doi => doi.Id)
            .ToListAsync());

        affectedDois.MatchSnapshot("affectedDomainOfInfluences");
    }

    [Fact]
    public Task TestAggregateNotFound()
        => Assert.ThrowsAsync<EntityNotFoundException>(async () => await TestEventPublisher.Publish(
            new DomainOfInfluenceUpdated
            {
                DomainOfInfluence = new DomainOfInfluenceEventData
                {
                    Id = DomainOfInfluenceMockedData.IdNotExisting,
                    Name = "St. Gallen Neu",
                    ShortName = "Sankt1",
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    ParentId = DomainOfInfluenceMockedData.IdBund,
                    Type = SharedProto.DomainOfInfluenceType.Ct,
                },
            }));

    [Fact]
    public async Task ShouldUpdateWithParties()
    {
        var response = await AdminClient.UpdateAsync(NewValidRootDoiRequest(x =>
            x.Parties.Add(new ProtoModels.DomainOfInfluenceParty
            {
                Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
            })));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceUpdated>();
        var partyCreated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePartyCreated>();
        var partyDeleted = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePartyDeleted>();

        partyDeleted.Id.Should().Be(DomainOfInfluenceMockedData.PartyIdBundAndere);

        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);

        partyCreated.Party.Id = string.Empty;
        partyCreated.MatchSnapshot("partyCreated");
        partyDeleted.MatchSnapshot("partyDeleted");
    }

    [Fact]
    public async Task SelfNonPoliticalTypeRootShouldPublishAndReturn()
    {
        var response = await AdminClient.UpdateAsync(new UpdateDomainOfInfluenceRequest
        {
            AdminRequest = new UpdateDomainOfInfluenceForAdminRequest
            {
                Id = DomainOfInfluenceMockedData.IdKirchgemeinde,
                Name = "Kk Neu",
                ShortName = "k",
                AuthorityName = "Gemeinde Uzwil",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                Type = (SharedProto.DomainOfInfluenceType)DomainOfInfluenceMockedData.Kirchgemeinde.Type,
                Canton = SharedProto.DomainOfInfluenceCanton.Tg,
                ContactPerson = new ProtoModels.ContactPerson(),
                PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(),
            },
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceUpdated>();

        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task ResponsibleForVotingCardsShouldReturn()
    {
        await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest());
        var votingCardDataUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceVotingCardDataUpdated>();
        votingCardDataUpdated.MatchSnapshot("votingCardDataUpdated");
    }

    [Fact]
    public async Task TestComparisonCountOfVotersCountingCircleEntriesShouldReturn()
    {
        await AdminClient.UpdateAsync(NewValidRequest(x => x.PlausibilisationConfiguration.ComparisonCountOfVotersCountingCircleEntries.Add(new ProtoModels.ComparisonCountOfVotersCountingCircleEntry
        {
            Category = SharedProto.ComparisonCountOfVotersCategory.A,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        })));
        var plausiConfigUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePlausibilisationConfigurationUpdated>();
        plausiConfigUpdated.MatchSnapshot("plausiConfigUpdated");
    }

    [Fact]
    public async Task NoCantonOnRootShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRootDoiRequest(x => x.Canton = SharedProto.DomainOfInfluenceCanton.Unspecified)),
            StatusCode.InvalidArgument,
            "Canton");
    }

    [Fact]
    public Task UpdatedType()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.Type = SharedProto.DomainOfInfluenceType.Sc)),
            StatusCode.InvalidArgument,
            "Type");

    [Fact]
    public Task ShouldThrowNotResponsibleForVotingCardsWithVotingCardData()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ResponsibleForVotingCards = false)),
            StatusCode.InvalidArgument);

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoReturnAddress()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ReturnAddress = null)),
            StatusCode.InvalidArgument,
            "ReturnAddress");

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoPrintData()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData = null)),
            StatusCode.InvalidArgument,
            "PrintData");

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoSwissPostData()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.SwissPostData = null)),
            StatusCode.InvalidArgument,
            "SwissPostData");

    [Fact]
    public Task ShouldThrowNoExternalPrintingCenterEaiMessageTypeWithExternalPrintingCenterSet()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ExternalPrintingCenterEaiMessageType = string.Empty)),
            StatusCode.InvalidArgument,
            "ExternalPrintingCenterEaiMessageType");

    [Fact]
    public async Task NoBfsForMunicipalityShouldTrow()
    {
        var req = NewValidRequest(x =>
        {
            x.Type = SharedProto.DomainOfInfluenceType.Mu;
            x.Bfs = string.Empty;
        });

        await AssertStatus(
            async () => await AdminClient.UpdateAsync(req),
            StatusCode.InvalidArgument,
            "Bfs");
    }

    [Fact]
    public async Task DuplicatedBfsForMunicipalityShouldTrow()
    {
        var req = NewValidRequest(x =>
        {
            x.Type = SharedProto.DomainOfInfluenceType.Mu;
            x.Bfs = DomainOfInfluenceMockedData.Gossau.Bfs;
        });

        await AssertStatus(
            async () => await AdminClient.UpdateAsync(req),
            StatusCode.AlreadyExists,
            "The bfs 3443 is already taken");
    }

    [Fact]
    public Task ShouldThrowNoPlausibilisationConfiguration()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.PlausibilisationConfiguration = null)),
            StatusCode.InvalidArgument,
            "PlausibilisationConfiguration");

    [Theory]
    [InlineData(SharedProto.VotingCardShippingFranking.Unspecified)]
    [InlineData(SharedProto.VotingCardShippingFranking.GasA)]
    [InlineData(SharedProto.VotingCardShippingFranking.GasB)]
    [InlineData(SharedProto.VotingCardShippingFranking.WithoutFranking)]
    public Task ShouldThrowResponsibleForVotingCardsInvalidShippingAway(SharedProto.VotingCardShippingFranking franking)
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingAway = franking)),
            StatusCode.InvalidArgument,
            "ShippingAway");

    [Theory]
    [InlineData(SharedProto.VotingCardShippingFranking.Unspecified)]
    [InlineData(SharedProto.VotingCardShippingFranking.B1)]
    [InlineData(SharedProto.VotingCardShippingFranking.B2)]
    [InlineData(SharedProto.VotingCardShippingFranking.A)]
    public Task ShouldThrowResponsibleForVotingCardsInvalidShippingReturn(SharedProto.VotingCardShippingFranking franking)
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingReturn = franking)),
            StatusCode.InvalidArgument,
            "ShippingReturn");

    [Fact]
    public Task NotExistingId()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o => o.Id = DomainOfInfluenceMockedData.IdNotExisting)),
            StatusCode.NotFound);

    [Fact]
    public Task ShouldThrowComparisonVoterParticipationConfigurationDuplicate()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
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
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowComparisonCountOfVotersConfigurationDuplicate()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations[0].Category = SharedProto.ComparisonCountOfVotersCategory.C)),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowComparisonCountOfVotersUnassignedCc()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersCountingCircleEntries.Add(new ProtoModels.ComparisonCountOfVotersCountingCircleEntry
                {
                    Category = SharedProto.ComparisonCountOfVotersCategory.A,
                    CountingCircleId = CountingCircleMockedData.IdGossau,
                }))),
            StatusCode.InvalidArgument,
            "must be assigned to the domain of influence");

    [Fact]
    public Task ShouldThrowComparisonVotingChannelConfigurationMissing()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonVotingChannelConfigurations");

    [Fact]
    public Task ShouldThrowComparisonVotingChannelConfigurationDuplicate()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations[0].VotingChannel = SharedProto.VotingChannel.EVoting)),
            StatusCode.InvalidArgument,
            "ComparisonVotingChannelConfigurations");

    [Fact]
    public Task ShouldThrowForeignParty()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.Parties.Add(new ProtoModels.DomainOfInfluenceParty
                {
                    Id = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Name = { LanguageUtil.MockAllLanguages("Andere") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("AN") },
                }))),
            StatusCode.InvalidArgument,
            "Some parties cannot be modified");

    [Fact]
    public async Task DuplicatedPartyShouldThrow()
    {
        var partyId = "0c03d15a-ab0a-439d-a5bb-91fb17d17cf1";

        await AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o =>
                {
                    o.Parties.Add(new ProtoModels.DomainOfInfluenceParty
                    {
                        Id = partyId,
                        Name = { LanguageUtil.MockAllLanguages("Andere") },
                        ShortDescription = { LanguageUtil.MockAllLanguages("AN") },
                    });
                    o.Parties.Add(new ProtoModels.DomainOfInfluenceParty
                    {
                        Id = partyId,
                        Name = { LanguageUtil.MockAllLanguages("Andere") },
                        ShortDescription = { LanguageUtil.MockAllLanguages("AN") },
                    });
                })),
            StatusCode.InvalidArgument,
            "domain of influence party can only be provided exactly once");
    }

    [Fact]
    public async Task ShouldThrowForeignExportConfiguration()
    {
        var exportConfigId = Guid.Parse("879049e5-f282-419c-b828-5110c322f1b6");

        await RunOnDb(async db =>
        {
            db.ExportConfigurations.Add(new()
            {
                Id = exportConfigId,
                DomainOfInfluenceId = DomainOfInfluenceMockedData.GuidGossau,
            });
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.ExportConfigurations.Add(new ProtoModels.ExportConfiguration
                {
                    Id = exportConfigId.ToString(),
                    Description = "Export Configuration",
                    EaiMessageType = "1234657",
                    Provider = SharedProto.ExportProvider.Seantis,
                });
            })),
            StatusCode.InvalidArgument,
            "Some export configurations cannot be modified");
    }

    [Fact]
    public async Task DuplicatedExportConfigurationShouldThrow()
    {
        var configId = "0c03d15a-ab0a-439d-a5bb-91fb17d17cf1";

        await AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
            {
                o.ExportConfigurations.Add(new ProtoModels.ExportConfiguration
                {
                    Id = configId,
                    Description = "Export Configuration",
                    EaiMessageType = "1234657",
                    Provider = SharedProto.ExportProvider.Seantis,
                });
                o.ExportConfigurations.Add(new ProtoModels.ExportConfiguration
                {
                    Id = configId,
                    Description = "Export Configuration2",
                    EaiMessageType = "1234658",
                    Provider = SharedProto.ExportProvider.Seantis,
                });
            })),
            StatusCode.InvalidArgument,
            "each export configuration can only be provided exactly once");
    }

    [Fact]
    public async Task ElectoralRegistrationEnabledWithoutResponsibleForVotingCardsShouldTrow()
    {
        var req = NewValidRequest(x =>
        {
            x.ResponsibleForVotingCards = false;
            x.ElectoralRegistrationEnabled = true;
        });

        await AssertStatus(
            async () => await AdminClient.UpdateAsync(req),
            StatusCode.InvalidArgument,
            "electoral registration");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
    }

    private static UpdateDomainOfInfluenceRequest NewValidRequest(
        Action<UpdateDomainOfInfluenceForAdminRequest>? customizer = null)
    {
        var request = new UpdateDomainOfInfluenceRequest
        {
            AdminRequest = new UpdateDomainOfInfluenceForAdminRequest
            {
                Id = DomainOfInfluenceMockedData.IdUzwil,
                Name = "Uzwil Neu",
                NameForProtocol = "Bezirk Uzwil",
                ShortName = "UzNeu",
                Canton = SharedProto.DomainOfInfluenceCanton.Sg, // Should be ignored unless root
                AuthorityName = "Gemeinde Uzwil",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                Type = SharedProto.DomainOfInfluenceType.Sk,
                Bfs = "3408",
                ResponsibleForVotingCards = false,
                ContactPerson = new ProtoModels.ContactPerson
                {
                    Email = "hans@muster.com",
                    Phone = "071 123 12 12",
                    FamilyName = "muster",
                    FirstName = "hans",
                    MobilePhone = "079 721 21 21",
                },
                PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(x =>
                {
                    x.ComparisonCountOfVotersConfigurations[0].ThresholdPercent = 20.8;
                    x.ComparisonVotingChannelConfigurations[0].ThresholdPercent = 42;
                }),
            },
        };
        customizer?.Invoke(request.AdminRequest);
        return request;
    }

    private static UpdateDomainOfInfluenceRequest NewValidResponsibleForVotingCardsRequest(
        Action<UpdateDomainOfInfluenceForAdminRequest>? customizer = null)
    {
        var request = new UpdateDomainOfInfluenceRequest
        {
            AdminRequest = new UpdateDomainOfInfluenceForAdminRequest
            {
                Id = DomainOfInfluenceMockedData.IdUzwil,
                Name = "Uzwil Neu",
                ShortName = "UzNeu",
                AuthorityName = "Gemeinde Uzwil",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                Type = SharedProto.DomainOfInfluenceType.Sk,
                ResponsibleForVotingCards = true,
                ExternalPrintingCenter = true,
                ExternalPrintingCenterEaiMessageType = "1234567",
                SapCustomerOrderNumber = "55431",
                ContactPerson = new ProtoModels.ContactPerson
                {
                    Email = "hans@muster.com",
                    Phone = "071 123 12 12",
                    FamilyName = "muster",
                    FirstName = "hans",
                    MobilePhone = "079 721 21 21",
                },
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
                    ShippingAway = SharedProto.VotingCardShippingFranking.A,
                    ShippingReturn = SharedProto.VotingCardShippingFranking.GasB,
                    ShippingMethod = SharedProto.VotingCardShippingMethod.OnlyPrintingPackagingToMunicipality,
                    ShippingVotingCardsToDeliveryAddress = true,
                },
                PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(),
                SwissPostData = new ProtoModels.DomainOfInfluenceVotingCardSwissPostData
                {
                    InvoiceReferenceNumber = "505964478",
                    FrankingLicenceReturnNumber = "965333145",
                },
                VotingCardColor = SharedProto.VotingCardColor.Green,
                ElectoralRegistrationEnabled = true,
            },
        };
        customizer?.Invoke(request.AdminRequest);
        return request;
    }

    private static UpdateDomainOfInfluenceRequest NewValidRootDoiRequest(
        Action<UpdateDomainOfInfluenceForAdminRequest>? customizer = null)
    {
        var request = new UpdateDomainOfInfluenceRequest
        {
            AdminRequest = new UpdateDomainOfInfluenceForAdminRequest
            {
                Id = DomainOfInfluenceMockedData.IdBund,
                Name = "Bund Neu",
                ShortName = "BundN",
                AuthorityName = "Bund",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                Type = SharedProto.DomainOfInfluenceType.Ch,
                Canton = SharedProto.DomainOfInfluenceCanton.Tg,
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
                            Id = DomainOfInfluenceMockedData.ExportConfigurationIdBund001,
                            Description = "Intf001-BUND-Updated",
                            ExportKeys =
                            {
                                AusmittlungXmlVoteTemplates.Ech0110.Key,
                                AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                            },
                            EaiMessageType = "1234567",
                            Provider = SharedProto.ExportProvider.Seantis,
                        },
                        new ProtoModels.ExportConfiguration
                        {
                            Description = "Intf100-BUND",
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
            },
        };
        customizer?.Invoke(request.AdminRequest);
        return request;
    }
}
