// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdatePartiesTest : BaseTest
{
    public DomainOfInfluenceUpdatePartiesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        await TestEventPublisher.Publish(new DomainOfInfluencePartyUpdated
        {
            EventInfo = GetMockedEventInfo(),
            Party = new DomainOfInfluencePartyEventData
            {
                Id = DomainOfInfluenceMockedData.PartyIdStGallenSP,
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                Name = { LanguageUtil.MockAllLanguages("Sozialdemokratische Partei edited") },
                ShortDescription = { LanguageUtil.MockAllLanguages("SP edited") },
            },
        });
        await TestEventPublisher.Publish(1, new DomainOfInfluencePartyCreated
        {
            EventInfo = GetMockedEventInfo(),
            Party = new DomainOfInfluencePartyEventData
            {
                Id = "7808f2f5-80ca-48ec-84ee-a0b376d5bf54",
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
            },
        });
        await TestEventPublisher.Publish(2, new DomainOfInfluencePartyDeleted
        {
            EventInfo = GetMockedEventInfo(),
            Id = DomainOfInfluenceMockedData.PartyIdStGallenSVP,
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });

        var parties = await RunOnDb(db => db.DomainOfInfluenceParties
            .Where(x => x.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidStGallen)
            .OrderBy(x => x.Name)
            .ToListAsync());

        parties.MatchSnapshot();
    }
}
