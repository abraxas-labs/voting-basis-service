// (c) Copyright by Abraxas Informatik AG
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
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(x =>
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
        var response = await CantonAdminClient.CreateAsync(NewValidRootDoiRequest());
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
                HasForeignerVoters = true,
                HasMinorVoters = true,
                SuperiorAuthorityDomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil,
                PublishResultsDisabled = true,
                HideLowerDomainOfInfluencesInReports = true,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var domainOfInfluences = await AdminClient.ListTreeAsync(new ListTreeDomainOfInfluenceRequest());
        domainOfInfluences.MatchSnapshot("domainOfInfluences");

        var doi = await CantonAdminClient.GetAsync(new GetDomainOfInfluenceRequest { Id = newId.ToString() });
        doi.SuperiorAuthorityDomainOfInfluence.Should().NotBeNull();
        doi.SuperiorAuthorityDomainOfInfluence.Id.Should().Be(DomainOfInfluenceMockedData.IdUzwil);

        var dbData = await RunOnDb(async db => await db.FindAsync<DomainOfInfluence>(newId));
        dbData!.CantonDefaults.MatchSnapshot("cantonDefaults");

        var permissions = await RunOnDb(db => db.DomainOfInfluencePermissions
            .OrderBy(x => x.TenantId)
            .ThenBy(x => x.DomainOfInfluenceId)
            .ToListAsync());
        foreach (var permission in permissions)
        {
            permission.CountingCircleIds.Sort();
        }

        permissions.MatchSnapshot("permissions", x => x.Id);

        var hierarchies = await RunOnDb(db => db.DomainOfInfluenceHierarchies
            .OrderBy(x => x.TenantId)
            .ThenBy(x => x.DomainOfInfluenceId)
            .ToListAsync());
        foreach (var hierarchy in hierarchies)
        {
            hierarchy.ChildIds.Sort();
        }

        hierarchies.MatchSnapshot("hierarchies", x => x.Id);
    }

    [Fact]
    public async Task TestChildDoiShouldGiveTenantAccessToParents()
    {
        var newId = Guid.Parse("3c3f3ae2-0439-4998-85ff-ae1f7eac94a5");
        var tenantId = "1345-mock";

        await TestEventPublisher.Publish(new DomainOfInfluenceCreated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = newId.ToString(),
                Name = "Test Neu",
                ShortName = "BZ Uz",
                Bfs = "3442",
                Code = "C3442",
                SortNumber = 53,
                SecureConnectId = tenantId,
                ParentId = DomainOfInfluenceMockedData.IdUzwil,
                Type = SharedProto.DomainOfInfluenceType.Bz,
                HasForeignerVoters = true,
                HasMinorVoters = true,
                SuperiorAuthorityDomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil,
                PublishResultsDisabled = true,
                HideLowerDomainOfInfluencesInReports = true,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var permissions = await RunOnDb(db => db.DomainOfInfluencePermissions
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.DomainOfInfluenceId)
            .ToListAsync());

        // The tenant should be added to all parent domain of influences
        permissions.Where(x => x.IsParent).Should().HaveCount(3);
        permissions.Where(x => !x.IsParent).Should().HaveCount(1);
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
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(x =>
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
    public async Task ShouldCreateWithPublishResultsDisabled()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(x =>
        {
            x.Type = SharedProto.DomainOfInfluenceType.Mu;
            x.PublishResultsDisabled = true;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();
        eventData.DomainOfInfluence.PublishResultsDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task DuplicatedPartyShouldThrow()
    {
        var partyId = "efd6fbe7-a874-4817-b416-aacd255d198a";
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.Parties.Add(new ProtoModels.DomainOfInfluenceParty
                {
                    Id = partyId,
                    Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
                });
                o.Parties.Add(new ProtoModels.DomainOfInfluenceParty
                {
                    Id = partyId,
                    Name = { LanguageUtil.MockAllLanguages("Demo") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("D") },
                });
            })),
            StatusCode.InvalidArgument,
            "domain of influence party can only be provided exactly once");
    }

    [Fact]
    public async Task VirtualTopLevelOnChildShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o => o.VirtualTopLevel = true)),
            StatusCode.InvalidArgument,
            "VirtualTopLevel");
    }

    [Fact]
    public async Task NonPoliticalShouldPublishAndReturnOk()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidRootDoiRequest(x => x.Type = SharedProto.DomainOfInfluenceType.Ki));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task ResponsibleForVotingCardsShouldReturn()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest());
        var doiCreated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();
        var votingCardDataUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceVotingCardDataUpdated>();

        votingCardDataUpdated.DomainOfInfluenceId.Should().Be(response.Id);
        votingCardDataUpdated.MatchSnapshot("votingCardDataUpdated", d => d.DomainOfInfluenceId);

        doiCreated.DomainOfInfluence.ElectoralRegisterMultipleEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task CantonAdminWithDifferentCantonRootDoiShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRootDoiRequest(req => req.Canton = SharedProto.DomainOfInfluenceCanton.Tg)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task InvalidParentShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o => o.ParentId = DomainOfInfluenceMockedData.IdNotExisting)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ParentNonPoliticalSelfNonPoliticalShouldReturnOk()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(o =>
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
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(o =>
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
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(o =>
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
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o => o.ParentId = DomainOfInfluenceMockedData.IdUzwil)),
            StatusCode.InvalidArgument,
            "political hierarchical order");
    }

    [Fact]
    public async Task ParentTypeLargerThanSelfShouldReturnOk()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidRequest(o => o.ParentId = DomainOfInfluenceMockedData.IdBund));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task NoCantonOnRootShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRootDoiRequest(o => o.Canton = SharedProto.DomainOfInfluenceCanton.Unspecified)),
            StatusCode.InvalidArgument,
            "canton is required to load canton settings for root doi");
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
            async () => await CantonAdminClient.CreateAsync(req),
            StatusCode.InvalidArgument,
            "Bfs");
    }

    [Fact]
    public async Task DuplicatedBfsForMunicipalityShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var doi = await db.DomainOfInfluences.AsTracking().SingleAsync(d => d.Name == DomainOfInfluenceMockedData.Uzwil.Name);
            doi.Type = DomainOfInfluenceType.Mu;
            await db.SaveChangesAsync();
        });

        var req = NewValidRequest(o =>
        {
            o.Type = SharedProto.DomainOfInfluenceType.Mu;
            o.Bfs = DomainOfInfluenceMockedData.Uzwil.Bfs;
        });

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(req),
            StatusCode.AlreadyExists,
            "The bfs 3408 is already taken");
    }

    [Fact]
    public Task ShouldThrowNotResponsibleForVotingCardsWithVotingCardData()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ResponsibleForVotingCards = false)),
            StatusCode.InvalidArgument);

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoReturnAddress()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ReturnAddress = null)),
            StatusCode.InvalidArgument,
            "ReturnAddress");

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoPrintData()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData = null)),
            StatusCode.InvalidArgument,
            "PrintData");

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoSwissPostData()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.SwissPostData = null)),
            StatusCode.InvalidArgument,
            "SwissPostData");

    [Fact]
    public Task ShouldThrowNoExternalPrintingCenterEaiMessageTypeWithExternalPrintingCenterSet()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ExternalPrintingCenterEaiMessageType = string.Empty)),
            StatusCode.InvalidArgument,
            "ExternalPrintingCenterEaiMessageType");

    [Fact]
    public Task ShouldThrowComparisonVoterParticipationConfigurationDuplicate()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(
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
            async () => await CantonAdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowComparisonCountOfVotersConfigurationDuplicate()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations[0].Category = SharedProto.ComparisonCountOfVotersCategory.C)),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowAnyComparisonCountOfVotersCc()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(
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
            async () => await CantonAdminClient.CreateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonVotingChannelConfigurations");

    [Fact]
    public Task ShouldThrowComparisonVotingChannelConfigurationDuplicate()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(
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
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingAway = franking)),
            StatusCode.InvalidArgument,
            "ShippingAway");

    [Theory]
    [InlineData(SharedProto.VotingCardShippingFranking.Unspecified)]
    [InlineData(SharedProto.VotingCardShippingFranking.B1)]
    [InlineData(SharedProto.VotingCardShippingFranking.B2)]
    [InlineData(SharedProto.VotingCardShippingFranking.A)]
    public Task ShouldThrowResponsibleForVotingCardsInvalidShippingReturn(SharedProto.VotingCardShippingFranking franking)
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingReturn = franking)),
            StatusCode.InvalidArgument,
            "ShippingReturn");

    [Fact]
    public async Task ShouldThrowForeignParty()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
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

    [Fact]
    public async Task ShouldThrowForeignExportConfiguration()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
            {
                o.ExportConfigurations.Add(new ProtoModels.ExportConfiguration
                {
                    Id = DomainOfInfluenceMockedData.ExportConfigurationIdBund001,
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
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
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
            async () => await CantonAdminClient.CreateAsync(req),
            StatusCode.InvalidArgument,
            "electoral registration");
    }

    [Fact]
    public async Task MultipleElectoralRegistersWithoutElectoralRegistrationShouldTrow()
    {
        var req = NewValidResponsibleForVotingCardsRequest(x =>
        {
            x.ResponsibleForVotingCards = true;
            x.ElectoralRegistrationEnabled = false;
            x.ElectoralRegisterMultipleEnabled = true;
        });

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(req),
            StatusCode.InvalidArgument,
            "multiple electoral registers");
    }

    [Fact]
    public async Task InternalPlausibilisationDisabledShouldThrow()
    {
        await ModifyDbEntities<CantonSettings>(x => x.Canton == DomainOfInfluenceCanton.Sg, x => x.InternalPlausibilisationDisabled = true);

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "internal plausibilisation is disabled for this canton");
    }

    [Fact]
    public async Task InternalPlausibilisationDisabledAsRootDoiShouldThrow()
    {
        await ModifyDbEntities<CantonSettings>(x => x.Canton == DomainOfInfluenceCanton.Sg, x => x.InternalPlausibilisationDisabled = true);

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRootDoiRequest()),
            StatusCode.InvalidArgument,
            "internal plausibilisation is disabled for this canton");
    }

    [Fact]
    public async Task NoPlausibilisationConfigurationShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(x => x.PlausibilisationConfiguration = null)),
            StatusCode.InvalidArgument,
            "plausibilisation configuration is required");
    }

    [Fact]
    public async Task NoPlausibilisationConfigurationWithDisabledCantonSettingsShouldReturnOk()
    {
        await ModifyDbEntities<CantonSettings>(x => x.Canton == DomainOfInfluenceCanton.Sg, x => x.InternalPlausibilisationDisabled = true);

        var response = await CantonAdminClient.CreateAsync(NewValidRequest(x => x.PlausibilisationConfiguration = null));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCreated>();

        eventData.DomainOfInfluence.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.DomainOfInfluence.Id);
    }

    [Fact]
    public async Task ForeignCantonSuperiorAuthorityShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(x => x.SuperiorAuthorityDomainOfInfluenceId = DomainOfInfluenceMockedData.IdZurich)),
            StatusCode.InvalidArgument,
            "Cannot set a domain of influence from a different canton as superior authority");
    }

    [Fact]
    public async Task InvalidSuperiorAuthorityTypeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(x => x.SuperiorAuthorityDomainOfInfluenceId = DomainOfInfluenceMockedData.IdUzwil)),
            StatusCode.InvalidArgument,
            "The selected superior authority domain of influence has an invalid type");
    }

    [Fact]
    public async Task NotExistingSuperiorAuthorityShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(x => x.SuperiorAuthorityDomainOfInfluenceId = Guid.Empty.ToString())),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task DisablePublishResultsOnNonCommunalShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(x =>
            {
                x.PublishResultsDisabled = true;
                x.Type = SharedProto.DomainOfInfluenceType.Ct;
            })),
            StatusCode.InvalidArgument,
            "Cannot disable publish results on non-communal domain of influence");
    }

    [Fact]
    public async Task DisablePublishResultsWhenCantonOptionDisabledShouldThrow()
    {
        await ModifyDbEntities<CantonSettings>(
            c => c.Canton == DomainOfInfluenceCanton.Sg,
            c => c.DomainOfInfluencePublishResultsOptionEnabled = false);

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(x =>
            {
                x.PublishResultsDisabled = true;
            })),
            StatusCode.InvalidArgument,
            "Canton does not allow to disable publish results");
    }

    [Fact]
    public async Task NoECollectingReferendumAndCommitteeValuesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(x => x.ECollectingReferendumMinSignatureCount = null)),
            StatusCode.InvalidArgument,
            "Referendum minimum signature count is required");

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(x => x.ECollectingReferendumMaxElectronicSignaturePercent = null)),
            StatusCode.InvalidArgument,
            "Referendum maximum electronic signature percent is required");

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(x => x.ECollectingInitiativeNumberOfMembersCommittee = null)),
            StatusCode.InvalidArgument,
            "Initiative number of members committee is required");

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(x => x.ECollectingEmail = string.Empty)),
            StatusCode.InvalidArgument,
            "ECollecting email is required");
    }

    [Fact]
    public async Task NoECollectingInitiativeValuesOnCommunalShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(x =>
            {
                x.Type = SharedProto.DomainOfInfluenceType.Mu;
                x.Bfs = "1111";
                x.ECollectingInitiativeMinSignatureCount = null;
            })),
            StatusCode.InvalidArgument,
            "Initiative minimum signature count is required for communal domain of influence");

        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidResponsibleForVotingCardsRequest(x =>
            {
                x.Type = SharedProto.DomainOfInfluenceType.Mu;
                x.Bfs = "1111";
                x.ECollectingInitiativeMaxElectronicSignaturePercent = null;
            })),
            StatusCode.InvalidArgument,
            "Initiative maximum electronic signature percent is required for communal domain of influence");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .CreateAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
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
            HasForeignerVoters = true,
            HasMinorVoters = true,
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
            SuperiorAuthorityDomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
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
            SapCustomerOrderNumber = "55431",
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
                ShippingVotingCardsToDeliveryAddress = true,
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
            SwissPostData = new ProtoModels.DomainOfInfluenceVotingCardSwissPostData
            {
                InvoiceReferenceNumber = "505964478",
                FrankingLicenceAwayNumber = "71025612",
                FrankingLicenceReturnNumber = "965333145",
            },
            ElectoralRegistrationEnabled = true,
            StistatMunicipality = true,
            VotingCardFlatRateDisabled = true,
            ElectoralRegisterMultipleEnabled = true,
            IsMainVotingCardsDomainOfInfluence = true,
            ECollectingEnabled = true,
            ECollectingInitiativeMinSignatureCount = 10000,
            ECollectingInitiativeMaxElectronicSignaturePercent = 50,
            ECollectingInitiativeNumberOfMembersCommittee = 15,
            ECollectingReferendumMinSignatureCount = 1000,
            ECollectingReferendumMaxElectronicSignaturePercent = 20,
            ECollectingEmail = "ecollecting@uzwil.ch",
        };
        customizer?.Invoke(request);
        return request;
    }
}
