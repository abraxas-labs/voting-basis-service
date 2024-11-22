// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateVotingCardDataTest : BaseTest
{
    public DomainOfInfluenceUpdateVotingCardDataTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestProcessor()
    {
        var id = DomainOfInfluenceMockedData.IdGossau;
        await TestEventPublisher.Publish(new DomainOfInfluenceVotingCardDataUpdated
        {
            DomainOfInfluenceId = id,
            ReturnAddress = new DomainOfInfluenceVotingCardReturnAddressEventData
            {
                AddressLine1 = "Updated Zeile 1",
                AddressLine2 = "Updated Zeile 2",
                AddressAddition = "Updated Addition",
                City = "Updated City",
                Country = "Updated Schweiz",
                Street = "Updated Street",
                ZipCode = "1000",
            },
            PrintData = new DomainOfInfluenceVotingCardPrintDataEventData
            {
                ShippingAway = SharedProto.VotingCardShippingFranking.A,
                ShippingReturn = SharedProto.VotingCardShippingFranking.GasB,
                ShippingMethod = SharedProto.VotingCardShippingMethod.OnlyPrintingPackagingToMunicipality,
                ShippingVotingCardsToDeliveryAddress = true,
            },
            ExternalPrintingCenter = true,
            ExternalPrintingCenterEaiMessageType = "GOSSAU-Updated",
            SapCustomerOrderNumber = "915421",
            SwissPostData = new DomainOfInfluenceVotingCardSwissPostDataEventData
            {
                InvoiceReferenceNumber = "505964478",
                FrankingLicenceReturnNumber = "965333145",
            },
            StistatMunicipality = true,
            EventInfo = GetMockedEventInfo(),
        });

        var doi = await GetDbEntity<DomainOfInfluence>(x => x.Id == DomainOfInfluenceMockedData.GuidGossau);
        var protoDoi = RunScoped<TestMapper, ProtoModels.DomainOfInfluence>(mapper => mapper.Map<ProtoModels.DomainOfInfluence>(doi));
        protoDoi.ReturnAddress.MatchSnapshot("returnAddress");
        protoDoi.PrintData.MatchSnapshot("printData");
        protoDoi.SwissPostData.MatchSnapshot("swissPostData");
        protoDoi.ExternalPrintingCenter.Should().BeTrue();
        protoDoi.ExternalPrintingCenterEaiMessageType.Should().Be("GOSSAU-Updated");
        protoDoi.SapCustomerOrderNumber.Should().Be("915421");
        protoDoi.StistatMunicipality.Should().BeTrue();
    }
}
