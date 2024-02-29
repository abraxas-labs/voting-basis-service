// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleListMergersTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private readonly Dictionary<string, string> _idsByName = new();
    private int _seederIdx;
    private int _eventNr;

    public CountingCircleListMergersTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
        await SeedMergers();
    }

    [Fact]
    public async Task ShouldList()
    {
        var mergers = await AdminClient.ListMergersAsync(new ListCountingCirclesMergersRequest());

        foreach (var merger in mergers.Mergers)
        {
            merger.Id = string.Empty;
            merger.NewCountingCircle.Id = string.Empty;
        }

        mergers.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldListMerged()
    {
        var mergers = await AdminClient.ListMergersAsync(new ListCountingCirclesMergersRequest { Merged = true });
        mergers.Mergers.Single().NewCountingCircle.Name.Should().Be("rappi-jona");
    }

    [Fact]
    public async Task ShouldListUnmerged()
    {
        var mergers = await AdminClient.ListMergersAsync(new ListCountingCirclesMergersRequest { Merged = false });
        mergers.Mergers.Single().NewCountingCircle.Name.Should().Be("uzwil-neu");
    }

    [Fact]
    public async Task ShouldListWithDeleted()
    {
        await TestEventPublisher.Publish(
            NextEventNr(),
            new CountingCircleDeleted { CountingCircleId = _idsByName["rappi-jona"] });
        var mergers = await AdminClient.ListMergersAsync(new ListCountingCirclesMergersRequest { Merged = true });

        foreach (var merger in mergers.Mergers)
        {
            merger.Id = string.Empty;
            merger.NewCountingCircle.Id = string.Empty;
        }

        mergers.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new CountingCircleService.CountingCircleServiceClient(channel)
            .ListMergersAsync(new ListCountingCirclesMergersRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }

    private async Task SeedMergers()
    {
        await SeedMerger(
            0,
            "rappi-jona",
            CountingCircleMockedData.IdRapperswil,
            CountingCircleMockedData.IdJona);
        await SeedMerger(
            1,
            "uzwil-neu",
            CountingCircleMockedData.IdUzwil,
            CountingCircleMockedData.IdUzwilKirche,
            CountingCircleMockedData.IdUzwilKircheAndere);
    }

    private async Task SeedMerger(int activeFromDelta, string name, params string[] mergedCcIds)
    {
        EventPublisherMock.Clear();

        var i = _seederIdx++;
        var id = await AdminClient.ScheduleMergerAsync(new ScheduleCountingCirclesMergerRequest
        {
            Bfs = "BFS M" + i,
            Code = "CODE M" + i,
            Name = name,
            ActiveFrom = MockedClock.GetTimestamp(activeFromDelta),
            ResponsibleAuthority = new ProtoModels.Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            },
            MergedCountingCircleIds =
                {
                    mergedCcIds,
                },
            CopyFromCountingCircleId = mergedCcIds[0],
        });
        _idsByName[name] = id.Id;

        var scheduledEvent = EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerScheduled>();
        await TestEventPublisher.Publish(NextEventNr(), scheduledEvent);

        var activatedEvents = EventPublisherMock.GetPublishedEvents<CountingCirclesMergerActivated>().ToArray();
        await TestEventPublisher.Publish(NextEventNr(activatedEvents.Length), activatedEvents);

        var mergedEvents = EventPublisherMock.GetPublishedEvents<CountingCircleMerged>().ToArray();
        await TestEventPublisher.Publish(NextEventNr(mergedEvents.Length), mergedEvents);
    }

    private int NextEventNr(int delta = 1)
    {
        var i = _eventNr;
        _eventNr += delta;
        return i;
    }
}
