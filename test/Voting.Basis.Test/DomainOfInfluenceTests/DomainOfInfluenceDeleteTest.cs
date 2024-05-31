// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceDeleteTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    private string? _authTestDoiId;

    public DomainOfInfluenceDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestInvalidGuid()
        => await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteDomainOfInfluenceRequest
            {
                Id = DomainOfInfluenceMockedData.IdInvalid,
            }),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task TestNotFound()
        => await AssertStatus(
            async () => await AdminClient.DeleteAsync(new DeleteDomainOfInfluenceRequest
            {
                Id = DomainOfInfluenceMockedData.IdNotExisting,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task TestShouldPublishDeletedEvent()
    {
        await AdminClient.DeleteAsync(new DeleteDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdUzwil,
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceDeleted>();

        eventData.DomainOfInfluenceId.Should().Be(DomainOfInfluenceMockedData.IdUzwil);
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestAggregateShouldRemoveFromDatabaseInclInheritedCountingCirclesOnParents()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EventInfo = GetMockedEventInfo(),
        });
        (await RunOnDb(db => db.DomainOfInfluences
                .FirstOrDefaultAsync(di => di.Id == DomainOfInfluenceMockedData.GuidGossau)))
            .Should()
            .BeNull();
        (await RunOnDb(db => db.DomainOfInfluenceCountingCircles
                .FirstOrDefaultAsync(di => di.CountingCircleId == Guid.Parse(CountingCircleMockedData.IdGossau))))
            .Should()
            .BeNull();
        (await RunOnDb(db => db.ExportConfigurations
                .FirstOrDefaultAsync(di => di.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidGossau)))
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task TestIndirectCascadeDeleteShouldWork()
    {
        // invalid cascade delete: doi delete doi partys but also proportional elections.
        // proportional election deletes candidates which depends on doi party.
        await RunOnDb(async db =>
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        });

        await ProportionalElectionMockedData.Seed(RunScoped);

        // should throw no exception
        await TestEventPublisher.Publish(new DomainOfInfluenceDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            EventInfo = GetMockedEventInfo(),
        });
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestDoiId == null)
        {
            var response = await AdminClient.CreateAsync(new CreateDomainOfInfluenceRequest
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
                    FrankingLicenceReturnNumber = "965333145",
                },
            });

            _authTestDoiId = response.Id;
            await RunEvents<DomainOfInfluenceCreated>();
        }

        await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .DeleteAsync(new DeleteDomainOfInfluenceRequest { Id = _authTestDoiId });
        _authTestDoiId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
    }
}
