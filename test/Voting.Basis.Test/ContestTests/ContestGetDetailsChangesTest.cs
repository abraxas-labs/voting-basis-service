// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Extensions;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestGetDetailsChangesTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestGetDetailsChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped, false);
        await ProportionalElectionMockedData.Seed(RunScoped, false);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped, false);
    }

    [Fact]
    public async Task ShouldNotifyAsElectionAdmin()
    {
        var electionGroupById = await GetElectionGroupById();

        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = GetUzwilClient(Roles.ElectionAdmin);
        var responseStream = client.GetDetailsChanges(
            new() { Id = ContestMockedData.IdBundContest },
            new(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(9, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(3), callCts.Token);

        var bundPb = VoteMockedData.BundVoteInContestBund;
        var uzwilPbInOtherContest = VoteMockedData.UzwilVoteInContestStGallen;
        var genfPb = VoteMockedData.GenfVoteInContestBundWithoutChilds;

        var bundPbUnion = ProportionalElectionUnionMockedData.BundUnion;
        var stGallenPbUnionInOtherContest = ProportionalElectionUnionMockedData.StGallenDifferentTenant;

        var sgEg = electionGroupById[Guid.Parse(MajorityElectionMockedData.ElectionGroupIdStGallenMajorityElectionInContestBund)];
        var kircheEg = electionGroupById[Guid.Parse(MajorityElectionMockedData.ElectionGroupIdKircheMajorityElectionInContestKirche)];

        // these should be processed
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusiness: bundPb.CreatePoliticalBusinessMessage(EntityState.Added)));
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusiness: bundPb.CreatePoliticalBusinessMessage(EntityState.Modified)));
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusiness: bundPb.CreatePoliticalBusinessMessage(EntityState.Deleted)));

        // this should be ignored (not in same contest)
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusiness: uzwilPbInOtherContest.CreatePoliticalBusinessMessage(EntityState.Added)));

        // this should be ignored (no read permissions)
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusiness: genfPb.CreatePoliticalBusinessMessage(EntityState.Added)));

        // these should be processed
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: bundPbUnion.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: bundPbUnion.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: bundPbUnion.CreateBaseEntityEvent(EntityState.Deleted)));

        // this should be ignored (not in same contest)
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: stGallenPbUnionInOtherContest.CreateBaseEntityEvent(EntityState.Added)));

        // these should be processed
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: sgEg.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: sgEg.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: sgEg.CreateBaseEntityEvent(EntityState.Deleted)));

        // this should be ignored (not in same contest)
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: kircheEg.CreateBaseEntityEvent(EntityState.Added)));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(9);
        responses.Count(x => x.PoliticalBusiness?.Data?.Id == bundPb.Id.ToString()).Should().Be(3);
        responses.Count(x => x.PoliticalBusiness?.Data?.Id == genfPb.Id.ToString()).Should().Be(0);
        responses.Count(x => x.PoliticalBusiness?.Data?.Id == uzwilPbInOtherContest.Id.ToString()).Should().Be(0);
        responses.Count(x => x.PoliticalBusinessUnion?.Data?.Id == bundPbUnion.Id.ToString()).Should().Be(3);
        responses.Count(x => x.PoliticalBusinessUnion?.Data?.Id == stGallenPbUnionInOtherContest.Id.ToString()).Should().Be(0);

        responses.Where(x => x.PoliticalBusiness != null).OrderBy(x => x.PoliticalBusiness.NewEntityState).MatchSnapshot("politicalBusinesses");
        responses.Where(x => x.PoliticalBusinessUnion != null).OrderBy(x => x.PoliticalBusinessUnion.NewEntityState).MatchSnapshot("politicalBusinessUnions");
        responses.Where(x => x.ElectionGroup != null).OrderBy(x => x.ElectionGroup.NewEntityState).MatchSnapshot("electionGroups");

        callCts.Cancel();
    }

    [Fact]
    public async Task ShouldNotifyAsAdmin()
    {
        var mapper = GetService<TestMapper>();
        var electionGroupById = await GetElectionGroupById();

        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = GetUzwilClient(Roles.Admin);
        var responseStream = client.GetDetailsChanges(
            new() { Id = ContestMockedData.IdBundContest },
            new(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(10, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(3), callCts.Token);

        var bundPb = mapper.Map<SimplePoliticalBusiness>(VoteMockedData.BundVoteInContestBund);
        var uzwilPbInOtherContest = mapper.Map<SimplePoliticalBusiness>(VoteMockedData.UzwilVoteInContestStGallen);
        var genfPb = mapper.Map<SimplePoliticalBusiness>(VoteMockedData.GenfVoteInContestBundWithoutChilds);

        var bundPbUnion = mapper.Map<SimplePoliticalBusinessUnion>(ProportionalElectionUnionMockedData.BundUnion);
        var stGallenPbUnionInOtherContest = mapper.Map<SimplePoliticalBusinessUnion>(ProportionalElectionUnionMockedData.StGallenDifferentTenant);

        var sgEg = electionGroupById[Guid.Parse(MajorityElectionMockedData.ElectionGroupIdStGallenMajorityElectionInContestBund)];
        var kircheEg = electionGroupById[Guid.Parse(MajorityElectionMockedData.ElectionGroupIdKircheMajorityElectionInContestKirche)];

        // these should be processed
        await PublishMessage(new ContestDetailsChangeMessage(bundPb.CreatePoliticalBusinessMessage(EntityState.Added)));
        await PublishMessage(new ContestDetailsChangeMessage(bundPb.CreatePoliticalBusinessMessage(EntityState.Modified)));
        await PublishMessage(new ContestDetailsChangeMessage(bundPb.CreatePoliticalBusinessMessage(EntityState.Deleted)));
        await PublishMessage(new ContestDetailsChangeMessage(genfPb.CreatePoliticalBusinessMessage(EntityState.Added)));

        // this should be ignored (not in same contest)
        await PublishMessage(new ContestDetailsChangeMessage(uzwilPbInOtherContest.CreatePoliticalBusinessMessage(EntityState.Added)));

        // these should be processed
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: bundPbUnion.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: bundPbUnion.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: bundPbUnion.CreateBaseEntityEvent(EntityState.Deleted)));

        // this should be ignored (not in same contest)
        await PublishMessage(new ContestDetailsChangeMessage(politicalBusinessUnion: stGallenPbUnionInOtherContest.CreateBaseEntityEvent(EntityState.Added)));

        // these should be processed
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: sgEg.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: sgEg.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: sgEg.CreateBaseEntityEvent(EntityState.Deleted)));

        // this should be ignored (not in same contest)
        await PublishMessage(new ContestDetailsChangeMessage(electionGroup: kircheEg.CreateBaseEntityEvent(EntityState.Added)));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(10);
        responses.Count(x => x.PoliticalBusiness?.Data?.Id == bundPb.Id.ToString()).Should().Be(3);
        responses.Count(x => x.PoliticalBusiness?.Data?.Id == genfPb.Id.ToString()).Should().Be(1);
        responses.Count(x => x.PoliticalBusiness?.Data?.Id == uzwilPbInOtherContest.Id.ToString()).Should().Be(0);
        responses.Count(x => x.PoliticalBusinessUnion?.Data?.Id == bundPbUnion.Id.ToString()).Should().Be(3);
        responses.Count(x => x.PoliticalBusinessUnion?.Data?.Id == stGallenPbUnionInOtherContest.Id.ToString()).Should().Be(0);

        responses.Where(x => x.PoliticalBusiness != null).OrderBy(x => x.PoliticalBusiness.NewEntityState).ThenBy(x => x.PoliticalBusiness.Data.Id).MatchSnapshot("politicalBusinesses");
        responses.Where(x => x.PoliticalBusinessUnion != null).OrderBy(x => x.PoliticalBusinessUnion.NewEntityState).MatchSnapshot("politicalBusinessUnions");
        responses.Where(x => x.ElectionGroup != null).OrderBy(x => x.ElectionGroup.NewEntityState).MatchSnapshot("electionGroups");

        callCts.Cancel();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ContestService.ContestServiceClient(channel).GetDetailsChanges(
            new() { Id = ContestMockedData.IdBundContest },
            new(cancellationToken: cts.Token));

        await responseStream.ResponseStream.ReadNIgnoreCancellation(1, cts.Token);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionAdminReadOnly;
        yield return Roles.ElectionSupporter;
    }

    private ContestService.ContestServiceClient GetUzwilClient(string role)
    {
        return new ContestService.ContestServiceClient(
            CreateGrpcChannel(
                tenant: DomainOfInfluenceMockedData.Uzwil.SecureConnectId,
                roles: role));
    }

    private async Task<Dictionary<Guid, ElectionGroup>> GetElectionGroupById()
    {
        var electionGroupRepo = GetService<IDbRepository<DataContext, ElectionGroup>>();

        return await electionGroupRepo.Query()
            .Include(e => e.PrimaryMajorityElection)
            .ToDictionaryAsync(e => e.Id, e => e);
    }
}
