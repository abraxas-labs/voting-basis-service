// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdatePlausibilisationConfigurationTest : BaseTest
{
    public DomainOfInfluenceUpdatePlausibilisationConfigurationTest(TestApplicationFactory factory)
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

        var plausiConfig = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(x =>
        {
            x.ComparisonCountOfVotersConfigurations[0].ThresholdPercent = 29.1;
            x.ComparisonVotingChannelConfigurations[0].ThresholdPercent = 29.1;
            x.ComparisonVoterParticipationConfigurations.Add(new ProtoModels.ComparisonVoterParticipationConfiguration
            {
                MainLevel = SharedProto.DomainOfInfluenceType.Ch,
                ComparisonLevel = SharedProto.DomainOfInfluenceType.Ch,
                ThresholdPercent = 4.5,
            });
            x.ComparisonCountOfVotersCountingCircleEntries.Add(new ProtoModels.ComparisonCountOfVotersCountingCircleEntry
            {
                CountingCircleId = CountingCircleMockedData.IdGossau,
                Category = SharedProto.ComparisonCountOfVotersCategory.B,
            });
        });

        var ev = new DomainOfInfluencePlausibilisationConfigurationUpdated
        {
            DomainOfInfluenceId = id,
            PlausibilisationConfiguration = RunScoped<TestMapper, PlausibilisationConfigurationEventData>(
                mapper => mapper.Map<PlausibilisationConfigurationEventData>(plausiConfig)),
            EventInfo = GetMockedEventInfo(),
        };

        await TestEventPublisher.Publish(ev);

        var plausiConfigData = await RunOnDb(db => db.PlausibilisationConfigurations
            .AsSplitQuery()
            .Include(x => x.ComparisonVoterParticipationConfigurations.OrderBy(y => y.MainLevel).ThenBy(y => y.ComparisonLevel))
            .Include(x => x.ComparisonCountOfVotersConfigurations.OrderBy(y => y.Category))
            .Include(x => x.ComparisonVotingChannelConfigurations.OrderBy(y => y.VotingChannel))
            .Include(x => x.DomainOfInfluence.CountingCircles.OrderBy(y => y.CountingCircle.Name))
            .SingleAsync(x => x.DomainOfInfluenceId == Guid.Parse(id)));

        plausiConfigData.DomainOfInfluence.CountingCircles
            .Count(doiCc => doiCc.ComparisonCountOfVotersCategory != ComparisonCountOfVotersCategory.Unspecified)
            .Should()
            .Be(1);

        var protoDoi = RunScoped<TestMapper, ProtoModels.DomainOfInfluence>(mapper => mapper.Map<ProtoModels.DomainOfInfluence>(plausiConfigData.DomainOfInfluence));
        protoDoi.PlausibilisationConfiguration.MatchSnapshot();
    }
}
