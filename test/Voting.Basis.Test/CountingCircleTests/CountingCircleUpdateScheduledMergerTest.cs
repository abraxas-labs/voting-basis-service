// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snapper;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleUpdateScheduledMergerTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string RapperswilJonaMergeId = "9dd859aa-2274-4a6a-bf19-6311e18644b2";
    private const string RapperswilJonaId = "102defb7-9fe7-426a-ad87-9127a650b79f";

    public CountingCircleUpdateScheduledMergerTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
        await SeedScheduledMerge();
    }

    [Fact]
    public async Task TestShouldPublishAndReturnOk()
    {
        await AdminClient.UpdateScheduledMergerAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerScheduleUpdated>();
        eventData.MatchSnapshot(x => x.Merger.Id, x => x.Merger.NewCountingCircle.Id);
    }

    [Fact]
    public async Task TestShouldPublishMergeIfTodayAndReturnOk()
    {
        await AdminClient.UpdateScheduledMergerAsync(NewValidRequest(x => x.ActiveFrom = MockedClock.GetTimestampDate()));
        var mergeEvent = EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerScheduleUpdated>();
        mergeEvent.Merger.Id.Should().NotBeEmpty();
        EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerActivated>()
            .Merger
            .Id
            .Should()
            .Be(mergeEvent.Merger.Id);
        EventPublisherMock.GetPublishedEvents<CountingCircleMerged>()
            .Select(x => x.CountingCircleId)
            .Should()
            .BeInAscendingOrder(CountingCircleMockedData.IdRapperswil, CountingCircleMockedData.IdJona);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(1, new CountingCirclesMergerScheduleUpdated
        {
            EventInfo = GetMockedEventInfo(),
            Merger = new CountingCirclesMergerEventData
            {
                Id = RapperswilJonaMergeId,
                NewCountingCircle = new CountingCircleEventData
                {
                    Id = RapperswilJonaId,
                    Name = "RapperswilJona",
                    Bfs = "bfs",
                    Code = "code",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                    },
                    NameForProtocol = "Stadt Rapperswil-Jona updated",
                    SortNumber = 200,
                },
                MergedCountingCircleIds =
                    {
                        CountingCircleMockedData.IdRapperswil,
                        CountingCircleMockedData.IdJona,
                    },
                CopyFromCountingCircleId = CountingCircleMockedData.IdRapperswil,
                ActiveFrom = MockedClock.GetTimestamp(10),
            },
        });

        var merger = await RunOnDb(db => db.CountingCirclesMergers.FirstAsync(x => x.Id == Guid.Parse(RapperswilJonaMergeId)));
        merger.ShouldMatchChildSnapshot("merger");

        var newCc = await RunOnDb(db => db.CountingCircles.IgnoreQueryFilters().FirstAsync(x => x.Id == Guid.Parse(RapperswilJonaId)));
        newCc.ShouldMatchChildSnapshot("newCc");

        var mergedCcs = await RunOnDb(db => db.CountingCircles
            .Where(x => x.Id == Guid.Parse(CountingCircleMockedData.IdRapperswil) || x.Id == Guid.Parse(CountingCircleMockedData.IdJona))
            .ToListAsync());
        mergedCcs.Count.Should().Be(2);
        mergedCcs.Select(x => x.MergeTargetId).Distinct().SingleOrDefault().Should().Be(Guid.Parse(RapperswilJonaMergeId));
    }

    [Fact]
    public Task NotExistingMergedCountingCircleId()
        => AssertStatus(
            async () => await AdminClient.UpdateScheduledMergerAsync(NewValidRequest(x => x.MergedCountingCircleIds.Add("2d1940fd-039b-437d-9d93-0e9af39a3551"))),
            StatusCode.InvalidArgument,
            "Some counting circle ids to merge do not exist or are duplicates");

    [Fact]
    public Task DuplicateMergedCountingCircleId()
        => AssertStatus(
            async () => await AdminClient.UpdateScheduledMergerAsync(NewValidRequest(x => x.MergedCountingCircleIds.Add(CountingCircleMockedData.IdRapperswil))),
            StatusCode.InvalidArgument,
            "Some counting circle ids to merge are duplicates");

    [Fact]
    public Task OnlyOneMergedCountingCircleId()
        => AssertStatus(
            async () => await AdminClient.UpdateScheduledMergerAsync(NewValidRequest(x => x.MergedCountingCircleIds.RemoveAt(1))),
            StatusCode.InvalidArgument,
            "Count");

    [Fact]
    public Task CopyFromIdNotInMergedCountingCircleIds()
        => AssertStatus(
            async () => await AdminClient.UpdateScheduledMergerAsync(NewValidRequest(x => x.CopyFromCountingCircleId = CountingCircleMockedData.IdGossau)),
            StatusCode.InvalidArgument);

    [Fact]
    public Task NotFound()
        => AssertStatus(
            async () => await AdminClient.UpdateScheduledMergerAsync(NewValidRequest(x => x.NewCountingCircleId = "3827a808-860d-4e3a-a289-d0f7dd48c887")),
            StatusCode.NotFound);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .UpdateScheduledMergerAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }

    private static UpdateScheduledCountingCirclesMergerRequest NewValidRequest(Action<UpdateScheduledCountingCirclesMergerRequest>? customizer = null)
    {
        var request = new UpdateScheduledCountingCirclesMergerRequest
        {
            NewCountingCircleId = RapperswilJonaId,
            Name = "RapperswilJona",
            Bfs = "bfs",
            Code = "code",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            },
            MergedCountingCircleIds =
                {
                    CountingCircleMockedData.IdRapperswil,
                    CountingCircleMockedData.IdJona,
                },
            CopyFromCountingCircleId = CountingCircleMockedData.IdRapperswil,
            ActiveFrom = MockedClock.GetTimestamp(10),
            NameForProtocol = "Stadt Rapperswil-Jona updated",
            SortNumber = 200,
        };

        customizer?.Invoke(request);
        return request;
    }

    private async Task SeedScheduledMerge()
    {
        var mergerProto = new ProtoModels.CountingCirclesMerger
        {
            Id = RapperswilJonaMergeId,
            NewCountingCircle = new ProtoModels.CountingCircle
            {
                Id = RapperswilJonaId,
                Name = "RapperswilJona",
                Bfs = "none",
                State = SharedProto.CountingCircleState.Inactive,
                ResponsibleAuthority = new ProtoModels.Authority
                {
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                },
                ContactPersonDuringEvent = new ProtoModels.ContactPerson
                {
                    Email = "uzwil-test@abraxas.ch",
                    Phone = "071 123 12 21",
                    MobilePhone = "071 123 12 31",
                    FamilyName = "Muster",
                    FirstName = "Hans",
                },
                ContactPersonAfterEvent = new ProtoModels.ContactPerson
                {
                    Email = "uzwil-test2@abraxas.ch",
                    Phone = "071 123 12 22",
                    MobilePhone = "071 123 12 33",
                    FamilyName = "Wichtig",
                    FirstName = "Rudolph",
                },
                NameForProtocol = "Stadt Rapperswil-Jona",
                SortNumber = 20000,
            },
            ActiveFrom = MockedClock.GetTimestampDate(1),
            MergedCountingCircles =
                {
                    new ProtoModels.CountingCircle { Id = CountingCircleMockedData.IdRapperswil },
                    new ProtoModels.CountingCircle { Id = CountingCircleMockedData.IdJona },
                },
            CopyFromCountingCircleId = CountingCircleMockedData.IdRapperswil,
        };

        var mergerEventData = RunScoped<TestMapper, CountingCirclesMergerEventData>(
            mapper => mapper.Map<CountingCirclesMergerEventData>(mergerProto));

        var domainMerger = RunScoped<TestMapper, CountingCirclesMerger>(
            mapper => mapper.Map<CountingCirclesMerger>(mergerProto));

        await RunScoped<IServiceProvider>(async sp =>
        {
            var permissionService = sp.GetRequiredService<PermissionService>();
            permissionService.SetAbraxasAuthIfNotAuthenticated();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();

            var aggregate = aggregateFactory.New<CountingCircleAggregate>();
            aggregate.ScheduleMergeFrom(domainMerger);
            await aggregateRepository.Save(aggregate);
        });

        await TestEventPublisher.Publish(new CountingCirclesMergerScheduled
        {
            Merger = mergerEventData,
            EventInfo = GetMockedEventInfo(),
        });
    }
}
