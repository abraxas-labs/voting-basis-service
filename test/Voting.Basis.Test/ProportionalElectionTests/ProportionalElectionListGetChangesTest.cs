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
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ProportionalElectionTests;

public class ProportionalElectionListGetChangesTest : BaseGrpcTest<ProportionalElectionService.ProportionalElectionServiceClient>
{
    public ProportionalElectionListGetChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldNotifyAsElectionAdmin()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = ElectionAdminUzwilClient.GetListChanges(
            new(),
            new(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(3, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(3), callCts.Token);

        var gossauList = await RunOnDb(db => db.ProportionalElectionLists
            .Include(x => x.ProportionalElection)
            .SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.ListIdGossauProportionalElectionInContestGossau)));
        gossauList.ProportionalElection.ProportionalElectionLists = null!;

        var uzwilList = await RunOnDb(db => db.ProportionalElectionLists
            .Include(x => x.ProportionalElection)
            .SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.ListIdUzwilProportionalElectionInContestUzwil)));
        uzwilList.ProportionalElection.ProportionalElectionLists = null!;

        // these should be processed
        await PublishMessage(new ProportionalElectionListChangeMessage(uzwilList.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new ProportionalElectionListChangeMessage(uzwilList.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new ProportionalElectionListChangeMessage(uzwilList.CreateBaseEntityEvent(EntityState.Deleted)));

        // this should be ignored (no access)
        await PublishMessage(new ProportionalElectionListChangeMessage(gossauList.CreateBaseEntityEvent(EntityState.Added)));

        var responses = await readResponsesTask;

        responses.Should().HaveCount(3);
        responses.Count(x => x.List?.Data?.Id == uzwilList.Id.ToString()).Should().Be(3);
        responses.OrderBy(x => x.List.NewEntityState).MatchSnapshot();

        callCts.Cancel();
    }

    [Fact]
    public async Task ShouldNotifyAsAdmin()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = AdminClient.GetListChanges(
            new(),
            new(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(4, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(3), callCts.Token);

        var gossauList = await RunOnDb(db => db.ProportionalElectionLists
            .Include(x => x.ProportionalElection)
            .SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.ListIdGossauProportionalElectionInContestGossau)));
        gossauList.ProportionalElection.ProportionalElectionLists = null!;

        var uzwilList = await RunOnDb(db => db.ProportionalElectionLists
            .Include(x => x.ProportionalElection)
            .SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.ListIdUzwilProportionalElectionInContestUzwil)));
        uzwilList.ProportionalElection.ProportionalElectionLists = null!;

        // these should be processed
        await PublishMessage(new ProportionalElectionListChangeMessage(gossauList.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new ProportionalElectionListChangeMessage(gossauList.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new ProportionalElectionListChangeMessage(gossauList.CreateBaseEntityEvent(EntityState.Deleted)));
        await PublishMessage(new ProportionalElectionListChangeMessage(uzwilList.CreateBaseEntityEvent(EntityState.Added)));

        var responses = await readResponsesTask;

        responses.Should().HaveCount(4);
        responses.Count(x => x.List?.Data?.Id == gossauList.Id.ToString()).Should().Be(3);
        responses.Count(x => x.List?.Data?.Id == uzwilList.Id.ToString()).Should().Be(1);
        responses.OrderBy(x => x.List.NewEntityState).ThenBy(x => x.List.Data.Id).MatchSnapshot();

        callCts.Cancel();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ProportionalElectionService.ProportionalElectionServiceClient(channel).GetListChanges(
            new(),
            new(cancellationToken: cts.Token));

        await responseStream.ResponseStream.ReadNIgnoreCancellation(1, cts.Token);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }
}
