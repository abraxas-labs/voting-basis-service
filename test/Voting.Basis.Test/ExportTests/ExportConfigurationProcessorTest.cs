// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Basis.Test.ExportTests;

public class ExportConfigurationProcessorTest : BaseTest
{
    public ExportConfigurationProcessorTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldProcessCreated()
    {
        var id = Guid.Parse("c6d2da12-c911-409e-b06b-203e08632efb");
        await TestEventPublisher.Publish(new ExportConfigurationCreated
        {
            Configuration = new ExportConfigurationEventData
            {
                Description = "my-test",
                Id = id.ToString(),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
                ExportKeys =
                    {
                        AusmittlungXmlVoteTemplates.Ech0110.Key,
                        AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                    },
                EaiMessageType = "001",
                Provider = ExportProvider.Seantis,
            },
            EventInfo = TestEventPublisherAdapter.GetMockedEventInfo(),
        });

        var config = await RunOnDb(db => db.ExportConfigurations.FirstAsync(x => x.Id == id));
        config.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldProcessUpdated()
    {
        var id = Guid.Parse(DomainOfInfluenceMockedData.ExportConfigurationIdBund001);
        await TestEventPublisher.Publish(new ExportConfigurationUpdated
        {
            Configuration = new ExportConfigurationEventData
            {
                Description = "my-test",
                Id = id.ToString(),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
                ExportKeys =
                    {
                        AusmittlungXmlVoteTemplates.Ech0110.Key,
                        AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                    },
                EaiMessageType = "001",
                Provider = ExportProvider.Unspecified, // should get converted to Standard
            },
            EventInfo = TestEventPublisherAdapter.GetMockedEventInfo(),
        });

        var config = await RunOnDb(db => db.ExportConfigurations.FirstAsync(x => x.Id == id));
        config.ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldProcessDeleted()
    {
        var id = Guid.Parse(DomainOfInfluenceMockedData.ExportConfigurationIdBund001);
        await TestEventPublisher.Publish(new ExportConfigurationDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
            ConfigurationId = id.ToString(),
            EventInfo = TestEventPublisherAdapter.GetMockedEventInfo(),
        });

        var hasConfig = await RunOnDb(db => db.ExportConfigurations.AnyAsync(x => x.Id == id));
        hasConfig.Should().BeFalse();
    }
}
