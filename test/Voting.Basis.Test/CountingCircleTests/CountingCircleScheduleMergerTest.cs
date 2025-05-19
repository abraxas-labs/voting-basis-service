// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Domain;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using PermissionService = Voting.Basis.Core.Services.Permission.PermissionService;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleScheduleMergerTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string RapperswilJonaMergeId = "9dd859aa-2274-4a6a-bf19-6311e18644b2";
    private const string RapperswilJonaId = "102defb7-9fe7-426a-ad87-9127a650b79f";

    public CountingCircleScheduleMergerTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldPublishAndReturnOk()
    {
        var id = await CantonAdminClient.ScheduleMergerAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerScheduled>();
        eventData.Merger.NewCountingCircle.Id.Should().Be(id.Id);
        eventData.Merger.Id = string.Empty;
        eventData.Merger.NewCountingCircle.Id = string.Empty;
        eventData.MatchSnapshot(x => x.Merger.Id, x => x.Merger.NewCountingCircle.Id);
    }

    [Fact]
    public async Task TestShouldPublishMergeIfTodayAndReturnOk()
    {
        await CantonAdminClient.ScheduleMergerAsync(NewValidRequest(x => x.ActiveFrom = MockedClock.GetTimestampDate()));
        var mergeEvent = EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerScheduled>();
        mergeEvent.Merger.Id.Should().NotBeEmpty();

        var activatedEvent = EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerActivated>();
        activatedEvent.Merger.Id.Should().Be(mergeEvent.Merger.Id);
        activatedEvent.Merger.Id = string.Empty;
        activatedEvent.Merger.NewCountingCircle.Id.Should().Be(mergeEvent.Merger.NewCountingCircle.Id);
        activatedEvent.Merger.NewCountingCircle.Id = string.Empty;
        activatedEvent.MatchSnapshot();

        EventPublisherMock.GetPublishedEvents<CountingCircleMerged>()
            .Select(x => x.CountingCircleId)
            .Should()
            .BeInAscendingOrder(CountingCircleMockedData.IdRapperswil, CountingCircleMockedData.IdJona);
    }

    [Fact]
    public async Task JobShouldActivateNewCountingCircleAndSetMergedOnOldCountingCircles()
    {
        await SeedScheduledMerge();

        // after schedule merge the new counting circle and the merged counting circles should not be editable.
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidUpdateRequest(x => x.Id = RapperswilJonaId)),
            StatusCode.InvalidArgument,
            "Modifications not allowed");

        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidUpdateRequest(x => x.Id = CountingCircleMockedData.IdJona)),
            StatusCode.FailedPrecondition,
            "counting circle is in a scheduled merge");

        await RunScoped<ActivateCountingCirclesMergeJob>(job => job.Run(CancellationToken.None));

        var mergeActivatedEvents = EventPublisherMock.GetPublishedEvents<CountingCirclesMergerActivated>();
        var mergedEvents = EventPublisherMock.GetPublishedEvents<CountingCircleMerged>();

        mergeActivatedEvents.MatchSnapshot("mergeActivated");
        mergedEvents.MatchSnapshot("merged");

        await TestEventPublisher.Publish(1, mergeActivatedEvents.ToArray());
        await TestEventPublisher.Publish(2, mergedEvents.ToArray());

        var ccListAfterActivated = await CantonAdminClient.ListAsync(new ListCountingCircleRequest());
        ccListAfterActivated.CountingCircles_.FirstOrDefault(cc => cc.Id == CountingCircleMockedData.IdRapperswil).Should().BeNull();
        ccListAfterActivated.CountingCircles_.FirstOrDefault(cc => cc.Id == CountingCircleMockedData.IdJona).Should().BeNull();
        ccListAfterActivated.CountingCircles_.FirstOrDefault(cc => cc.Id == RapperswilJonaId).MatchSnapshot("rapperswil-jona");

        var rapperswilJona = await RunOnDb(db => db.CountingCircles
            .Include(cc => cc.DomainOfInfluences)
            .Include(cc => cc.MergeOrigin)
            .FirstOrDefaultAsync(cc => cc.Id == Guid.Parse(RapperswilJonaId)));

        var assignedDoiIds = rapperswilJona!.DomainOfInfluences
            .Select(doiCc => doiCc.DomainOfInfluenceId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        assignedDoiIds.MatchSnapshot("assignedDoiIds");
        rapperswilJona.MergeOrigin!.Merged.Should().BeTrue();

        // The permission should reflect the merge
        var permissions = await RunOnDb(db => db.DomainOfInfluencePermissions
            .OrderBy(x => x.TenantId)
            .ThenBy(x => x.DomainOfInfluenceId)
            .ToListAsync());
        foreach (var permission in permissions)
        {
            permission.CountingCircleIds.Sort();
        }

        permissions.MatchSnapshot("permissions", x => x.Id);

        // after activated merge the new counting circle should be editable and the merged counting circles should be deleted.
        await CantonAdminClient.UpdateAsync(NewValidUpdateRequest(x => x.Id = RapperswilJonaId));

        var rapperswilJonaUpdateAfterActivatedMergeUpdate = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        rapperswilJonaUpdateAfterActivatedMergeUpdate.MatchSnapshot("updateAfterActivatedMerge");

        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidUpdateRequest()),
            StatusCode.InvalidArgument,
            "Modifications not allowed");
    }

    [Fact]
    public Task NotExistingMergedCountingCircleId()
        => AssertStatus(
            async () => await CantonAdminClient.ScheduleMergerAsync(NewValidRequest(x => x.MergedCountingCircleIds.Add("2d1940fd-039b-437d-9d93-0e9af39a3551"))),
            StatusCode.InvalidArgument,
            "Some counting circle ids to merge do not exist or are duplicates");

    [Fact]
    public Task DuplicateMergedCountingCircleId()
        => AssertStatus(
            async () => await CantonAdminClient.ScheduleMergerAsync(NewValidRequest(x => x.MergedCountingCircleIds.Add(CountingCircleMockedData.IdRapperswil))),
            StatusCode.InvalidArgument,
            "Some counting circle ids to merge are duplicates");

    [Fact]
    public Task OnlyOneMergedCountingCircleId()
        => AssertStatus(
            async () => await CantonAdminClient.ScheduleMergerAsync(NewValidRequest(x => x.MergedCountingCircleIds.RemoveAt(1))),
            StatusCode.InvalidArgument,
            "Count");

    [Fact]
    public Task CopyFromIdNotInMergedCountingCircleIds()
        => AssertStatus(
            async () => await CantonAdminClient.ScheduleMergerAsync(NewValidRequest(x => x.CopyFromCountingCircleId = CountingCircleMockedData.IdGossau)),
            StatusCode.InvalidArgument);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .ScheduleMergerAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
    }

    private static ScheduleCountingCirclesMergerRequest NewValidRequest(Action<ScheduleCountingCirclesMergerRequest>? customizer = null)
    {
        var request = new ScheduleCountingCirclesMergerRequest
        {
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
            NameForProtocol = "Stadt Rapperswil-Jona",
            SortNumber = 40,
        };

        customizer?.Invoke(request);
        return request;
    }

    private static UpdateCountingCircleRequest NewValidUpdateRequest(Action<UpdateCountingCircleRequest>? action = null)
    {
        var request = new UpdateCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdRapperswil,
            Name = "rapperswil jona",
            Bfs = "bfs",
            Code = "code",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                Name = "Rapperswil Jona",
                Email = "rapperswil@abraxas.ch",
                Phone = "072 123 12 20",
                Street = "WerkstrasseX",
                City = "Rapperswil",
                Zip = "8640",
                SecureConnectId = TestDefaults.TenantId,
            },
            ContactPersonSameDuringEventAsAfter = false,
            ContactPersonDuringEvent = new ProtoModels.ContactPerson
            {
                Email = "rapperswil@abraxas.ch",
                Phone = "072 123 12 21",
                MobilePhone = "072 123 12 31",
                FamilyName = "Muster-2",
                FirstName = "Hans-2",
            },
            ContactPersonAfterEvent = new ProtoModels.ContactPerson
            {
                Email = "rapperswil@abraxas.ch",
                Phone = "072 123 12 21",
                MobilePhone = "072 123 12 33",
                FamilyName = "Wichtig-2",
                FirstName = "Rudolph-2",
            },
            NameForProtocol = "Stadt Rapperswil-Jona",
            SortNumber = 40,
            Canton = SharedProto.DomainOfInfluenceCanton.Sg,
        };

        action?.Invoke(request);
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
                NameForProtocol = "RapperswilJona",
                SortNumber = 22,
                Canton = SharedProto.DomainOfInfluenceCanton.Sg,
            },
            ActiveFrom = MockedClock.GetTimestampDate(),
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
            mapper => mapper.Map<CountingCirclesMerger>(mergerEventData));

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
