// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceValidateShortNameTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceValidateShortNameTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
        await RunOnDb(async db =>
        {
            var doi = await db.DomainOfInfluences.FirstAsync(x => x.Id == DomainOfInfluenceMockedData.GuidUzwil);
            doi.ResponsibleForVotingCards = true;
            doi.PrintData = new Data.Models.DomainOfInfluenceVotingCardPrintData
            {
                ShippingAway = VotingCardShippingFranking.B2,
                ShippingReturn = VotingCardShippingFranking.GasB,
                ShippingMethod = VotingCardShippingMethod.OnlyPrintingPackagingToMunicipality,
                ShippingVotingCardsToDeliveryAddress = true,
            };
            doi.ReturnAddress = new DomainOfInfluenceVotingCardReturnAddress
            {
                AddressLine1 = "Stadtverwaltung Uzwil",
                Street = "Bahnhofplatz 1",
                ZipCode = "9040",
                City = "Uzwil",
                Country = "Schweiz",
            };
            doi.ExternalPrintingCenter = true;
            doi.ExternalPrintingCenterEaiMessageType = "EAI-Uzwil";
            doi.SapCustomerOrderNumber = "0005400492";
            doi.SwissPostData = new DomainOfInfluenceVotingCardSwissPostData
            {
                InvoiceReferenceNumber = "958473825",
                FrankingLicenceAwayNumber = "73155598",
                FrankingLicenceReturnNumber = "562984257",
            };
            db.Update(doi);
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task TestAsCantonAdminWithValidShortnameShouldWork()
    {
        var response = await CantonAdminClient.ValidateShortNameAsync(new ValidateShortNameDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdGossau,
            Type = Abraxas.Voting.Basis.Shared.V1.DomainOfInfluenceType.Sk,
            ShortName = "SG00",
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsCantonAdminWithInvalidShortnameShouldWork()
    {
        var response = await CantonAdminClient.ValidateShortNameAsync(new ValidateShortNameDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdGossau,
            Type = Abraxas.Voting.Basis.Shared.V1.DomainOfInfluenceType.Sk,
            ShortName = "UZW",
        });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .ListAsync(new ListDomainOfInfluenceRequest
            {
                CountingCircleId = CountingCircleMockedData.IdStGallen,
            });

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
        yield return Roles.EVotingAdmin;
    }
}
