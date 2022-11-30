// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateForElectionAdminTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceUpdateForElectionAdminTest(TestApplicationFactory factory)
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
        var request = NewValidRequest(x =>
        {
            x.PlausibilisationConfiguration.ComparisonVoterParticipationConfigurations.Add(new ProtoModels.ComparisonVoterParticipationConfiguration
            {
                MainLevel = SharedProto.DomainOfInfluenceType.Ct,
                ComparisonLevel = SharedProto.DomainOfInfluenceType.Ch,
                ThresholdPercent = 2.0,
            });
        });

        await ElectionAdminUzwilClient.UpdateAsync(request);

        var plausiConfigUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePlausibilisationConfigurationUpdated>();
        plausiConfigUpdated.MatchSnapshot("plausiConfigUpdated");

        var contactPersonUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceContactPersonUpdated>();
        contactPersonUpdated.MatchSnapshot("contactPersonUpdated");

        var partyCreated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePartyCreated>();
        partyCreated.MatchSnapshot("partyCreated", x => x.Party.Id);
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
        var client = CreateAuthorizedClient(DomainOfInfluenceMockedData.Bund.SecureConnectId, Roles.ElectionAdmin);
        await client.UpdateAsync(NewValidRootDoiRequest(x =>
            x.Parties.Add(new ProtoModels.DomainOfInfluenceParty
            {
                Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
            })));

        var partyCreated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePartyCreated>();
        var partyDeleted = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePartyDeleted>();

        partyDeleted.Id.Should().Be(DomainOfInfluenceMockedData.PartyIdBundAndere);

        partyCreated.Party.Id = string.Empty;
        partyCreated.MatchSnapshot("partyCreated");
        partyDeleted.MatchSnapshot("partyDeleted");
    }

    [Fact]
    public async Task ResponsibleForVotingCardsShouldReturn()
    {
        await ElectionAdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest());
        var votingCardDataUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceVotingCardDataUpdated>();
        votingCardDataUpdated.MatchSnapshot("votingCardDataUpdated");
    }

    [Fact]
    public async Task TestComparisonCountOfVotersCountingCircleEntriesShouldReturn()
    {
        await ElectionAdminUzwilClient.UpdateAsync(NewValidRequest(x => x.PlausibilisationConfiguration.ComparisonCountOfVotersCountingCircleEntries.Add(new ProtoModels.ComparisonCountOfVotersCountingCircleEntry
        {
            Category = SharedProto.ComparisonCountOfVotersCategory.C,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        })));

        var plausiConfigUpdated = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluencePlausibilisationConfigurationUpdated>();
        plausiConfigUpdated.MatchSnapshot("plausiConfigUpdated");
    }

    [Fact]
    public Task OtherTenantShouldThrow()
        => AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateAsync(NewValidRequest(o => o.Id = DomainOfInfluenceMockedData.IdStGallen)),
            StatusCode.PermissionDenied);

    [Fact]
    public Task ShouldThrowNoContactPerson()
        => AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateAsync(NewValidRequest(o => o.ContactPerson = null)),
            StatusCode.InvalidArgument,
            "ContactPerson");

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoReturnAddress()
        => AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ReturnAddress = null)),
            StatusCode.InvalidArgument,
            "ReturnAddress");

    [Fact]
    public Task ShouldThrowResponsibleForVotingCardsNoPrintData()
        => AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData = null)),
            StatusCode.InvalidArgument,
            "PrintData");

    [Fact]
    public Task ShouldThrowNoExternalPrintingCenterEaiMessageTypeWithExternalPrintingCenterSet()
        => AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.ExternalPrintingCenterEaiMessageType = string.Empty)),
            StatusCode.InvalidArgument,
            "ExternalPrintingCenterEaiMessageType");

    [Theory]
    [InlineData(SharedProto.VotingCardShippingFranking.Unspecified)]
    [InlineData(SharedProto.VotingCardShippingFranking.GasA)]
    [InlineData(SharedProto.VotingCardShippingFranking.GasB)]
    [InlineData(SharedProto.VotingCardShippingFranking.WithoutFranking)]
    public Task ShouldThrowResponsibleForVotingCardsInvalidShippingAway(SharedProto.VotingCardShippingFranking franking)
        => AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingAway = franking)),
            StatusCode.InvalidArgument,
            "ShippingAway");

    [Theory]
    [InlineData(SharedProto.VotingCardShippingFranking.Unspecified)]
    [InlineData(SharedProto.VotingCardShippingFranking.B1)]
    [InlineData(SharedProto.VotingCardShippingFranking.B2)]
    [InlineData(SharedProto.VotingCardShippingFranking.A)]
    public Task ShouldThrowResponsibleForVotingCardsInvalidShippingReturn(SharedProto.VotingCardShippingFranking franking)
        => AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidResponsibleForVotingCardsRequest(o => o.PrintData.ShippingReturn = franking)),
            StatusCode.InvalidArgument,
            "ShippingReturn");

    [Fact]
    public Task NotExistingId()
        => AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequest(o => o.Id = DomainOfInfluenceMockedData.IdNotExisting)),
            StatusCode.NotFound);

    [Fact]
    public Task ShouldThrowComparisonVoterParticipationConfigurationDuplicate()
        => AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateAsync(
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
            async () => await ElectionAdminUzwilClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowComparisonCountOfVotersConfigurationDuplicate()
        => AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations[0].Category = SharedProto.ComparisonCountOfVotersCategory.C)),
            StatusCode.InvalidArgument,
            "ComparisonCountOfVotersConfigurations");

    [Fact]
    public Task ShouldThrowComparisonCountOfVotersUnassignedCc()
        => AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(
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
            async () => await ElectionAdminUzwilClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations.RemoveAt(0))),
            StatusCode.InvalidArgument,
            "ComparisonVotingChannelConfigurations");

    [Fact]
    public Task ShouldThrowComparisonVotingChannelConfigurationDuplicate()
        => AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateAsync(
                NewValidRequest(o => o.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations[0].VotingChannel = SharedProto.VotingChannel.EVoting)),
            StatusCode.InvalidArgument,
            "ComparisonVotingChannelConfigurations");

    [Fact]
    public Task ShouldThrowForeignParty()
        => AssertStatus(
            async () => await ElectionAdminUzwilClient.UpdateAsync(
                NewValidRequest(o => o.Parties.Add(new ProtoModels.DomainOfInfluenceParty
                {
                    Id = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Name = { LanguageUtil.MockAllLanguages("Andere") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("AN") },
                }))),
            StatusCode.InvalidArgument,
            "Some parties cannot be modified");

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private static UpdateDomainOfInfluenceRequest NewValidRequest(
        Action<UpdateDomainOfInfluenceForElectionAdminRequest>? customizer = null)
    {
        var request = new UpdateDomainOfInfluenceRequest
        {
            ElectionAdminRequest = new UpdateDomainOfInfluenceForElectionAdminRequest
            {
                Id = DomainOfInfluenceMockedData.IdUzwil,
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
                Parties =
                    {
                        new ProtoModels.DomainOfInfluenceParty
                        {
                            Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                            ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
                        },
                    },
            },
        };
        customizer?.Invoke(request.ElectionAdminRequest);
        return request;
    }

    private static UpdateDomainOfInfluenceRequest NewValidRootDoiRequest(
        Action<UpdateDomainOfInfluenceForElectionAdminRequest>? customizer = null)
    {
        var request = new UpdateDomainOfInfluenceRequest
        {
            ElectionAdminRequest = new UpdateDomainOfInfluenceForElectionAdminRequest
            {
                Id = DomainOfInfluenceMockedData.IdBund,
                ContactPerson = new ProtoModels.ContactPerson
                {
                    Email = "hans@muster.com",
                    Phone = "071 123 12 12",
                    FamilyName = "muster",
                    FirstName = "hans",
                    MobilePhone = "079 721 21 21",
                },
                PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(),
            },
        };
        customizer?.Invoke(request.ElectionAdminRequest);
        return request;
    }

    private static UpdateDomainOfInfluenceRequest NewValidResponsibleForVotingCardsRequest(
        Action<UpdateDomainOfInfluenceForElectionAdminRequest>? customizer = null)
    {
        var request = new UpdateDomainOfInfluenceRequest
        {
            ElectionAdminRequest = new UpdateDomainOfInfluenceForElectionAdminRequest
            {
                Id = DomainOfInfluenceMockedData.IdGossau,
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
                },
                ExternalPrintingCenter = true,
                ExternalPrintingCenterEaiMessageType = "1234567",
                PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(),
            },
        };
        customizer?.Invoke(request.ElectionAdminRequest);
        return request;
    }
}
