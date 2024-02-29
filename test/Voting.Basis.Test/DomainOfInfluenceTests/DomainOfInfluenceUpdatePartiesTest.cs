// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
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
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        var candidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestBund);
        var candidateBefore = await RunOnDb(db => db.ProportionalElectionCandidates.SingleAsync(x => x.Id == candidateId));
        candidateBefore.PartyId.Should().Be(DomainOfInfluenceMockedData.GuidPartyStGallenSP);

        await TestEventPublisher.Publish(new DomainOfInfluencePartyUpdated
        {
            EventInfo = GetMockedEventInfo(),
            Party = new DomainOfInfluencePartyEventData
            {
                Id = DomainOfInfluenceMockedData.PartyIdStGallenSVP,
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                Name = { LanguageUtil.MockAllLanguages("Schweizerische Volkspartei edited") },
                ShortDescription = { LanguageUtil.MockAllLanguages("SVP edited") },
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
            Id = DomainOfInfluenceMockedData.PartyIdStGallenSP,
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });

        var parties = await RunOnDb(db => db.DomainOfInfluenceParties
            .Where(x => x.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidStGallen)
            .OrderBy(x => x.Name)
            .ToListAsync());

        parties.MatchSnapshot();

        var candidateAfter = await RunOnDb(db => db.ProportionalElectionCandidates.Include(c => c.Party).SingleAsync(x => x.Id == candidateId));
        candidateAfter.PartyId.Should().Be(DomainOfInfluenceMockedData.GuidPartyStGallenSP); // Parties are soft deleted;
        candidateAfter.Party.Should().BeNull();
    }
}
