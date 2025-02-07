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
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging.Extensions;
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleGetChangesTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    public CountingCircleGetChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await CountingCircleMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldNotifyAsElectionAdmin()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = ElectionAdminClient.GetChanges(
            new(),
            new(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(3, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(3), callCts.Token);

        var bundCountingCircle = CountingCircleMockedData.Bund;
        var uzwilCountingCircle = CountingCircleMockedData.Uzwil;

        // these should be processed
        await PublishMessage(new CountingCircleChangeMessage(uzwilCountingCircle.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new CountingCircleChangeMessage(uzwilCountingCircle.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new CountingCircleChangeMessage(uzwilCountingCircle.CreateBaseEntityEvent(EntityState.Deleted)));

        // this should be ignored (no access)
        await PublishMessage(new CountingCircleChangeMessage(bundCountingCircle.CreateBaseEntityEvent(EntityState.Added)));

        var responses = await readResponsesTask;

        responses.Should().HaveCount(3);
        responses.Count(x => x.CountingCircle?.Data?.Id == uzwilCountingCircle.Id.ToString()).Should().Be(3);
        responses.OrderBy(x => x.CountingCircle.NewEntityState).MatchSnapshot();

        callCts.Cancel();
    }

    [Fact]
    public async Task ShouldNotifyAsAdmin()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = AdminClient.GetChanges(
            new(),
            new(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(4, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(3), callCts.Token);

        var bundCountingCircle = CountingCircleMockedData.Bund;
        var uzwilCountingCircle = CountingCircleMockedData.Uzwil;

        // these should be processed
        await PublishMessage(new CountingCircleChangeMessage(bundCountingCircle.CreateBaseEntityEvent(EntityState.Added)));
        await PublishMessage(new CountingCircleChangeMessage(bundCountingCircle.CreateBaseEntityEvent(EntityState.Modified)));
        await PublishMessage(new CountingCircleChangeMessage(bundCountingCircle.CreateBaseEntityEvent(EntityState.Deleted)));
        await PublishMessage(new CountingCircleChangeMessage(uzwilCountingCircle.CreateBaseEntityEvent(EntityState.Added)));

        var responses = await readResponsesTask;

        responses.Should().HaveCount(4);
        responses.Count(x => x.CountingCircle?.Data?.Id == bundCountingCircle.Id.ToString()).Should().Be(3);
        responses.Count(x => x.CountingCircle?.Data?.Id == uzwilCountingCircle.Id.ToString()).Should().Be(1);
        responses.OrderBy(x => x.CountingCircle.NewEntityState).ThenBy(x => x.CountingCircle.Data.Id).MatchSnapshot();

        callCts.Cancel();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new CountingCircleService.CountingCircleServiceClient(channel).GetChanges(
            new(),
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
}
