// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using CountingCirclesMerger = Voting.Basis.Core.Domain.CountingCirclesMerger;
using PermissionService = Voting.Basis.Core.Services.Permission.PermissionService;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleDeleteScheduledMergerTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string RapperswilJonaMergeId = "9dd859aa-2274-4a6a-bf19-6311e18644b2";
    private const string RapperswilJonaId = "102defb7-9fe7-426a-ad87-9127a650b79f";
    private bool _authTestNeedsSeeding = true;

    public CountingCircleDeleteScheduledMergerTest(TestApplicationFactory factory)
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
        await SeedScheduledMerge(1);
        await CantonAdminClient.DeleteScheduledMergerAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCirclesMergerScheduleDeleted>();
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task AlreadyMergedShouldThrow()
    {
        await SeedScheduledMerge();
        await AssertStatus(
            async () => await CantonAdminClient.DeleteScheduledMergerAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "The merger is already active");
    }

    [Fact]
    public Task NoActiveMergerShouldThrow()
        => AssertStatus(
            async () => await CantonAdminClient.DeleteScheduledMergerAsync(new DeleteScheduledCountingCirclesMergerRequest
            {
                NewCountingCircleId = CountingCircleMockedData.IdGossau,
            }),
            StatusCode.InvalidArgument,
            "No merger set, cannot delete");

    [Fact]
    public async Task TestCantonAdminOtherCantonShouldThrow()
    {
        await SeedScheduledMerge(1, SharedProto.DomainOfInfluenceCanton.Zh);
        await AssertStatus(
            async () => await CantonAdminClient.DeleteScheduledMergerAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await SeedScheduledMerge(1);
        await TestEventPublisher.Publish(1, new CountingCirclesMergerScheduleDeleted
        {
            EventInfo = GetMockedEventInfo(),
            MergerId = RapperswilJonaMergeId,
            NewCountingCircleId = RapperswilJonaId,
        });

        var hasMergers = await RunOnDb(db => db.CountingCirclesMergers.AnyAsync());
        hasMergers.Should().BeFalse();

        var hasCc = await RunOnDb(db => db.CountingCircles.IgnoreQueryFilters().AnyAsync(x => x.Id == Guid.Parse(RapperswilJonaId)));
        hasCc.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestNeedsSeeding)
        {
            await SeedScheduledMerge(1);
            _authTestNeedsSeeding = false;
        }

        await new CountingCircleService.CountingCircleServiceClient(channel)
            .DeleteScheduledMergerAsync(NewValidRequest());
        await RunEvents<CountingCirclesMergerScheduleDeleted>();
        _authTestNeedsSeeding = true;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
    }

    private static DeleteScheduledCountingCirclesMergerRequest NewValidRequest()
    {
        return new DeleteScheduledCountingCirclesMergerRequest
        {
            NewCountingCircleId = RapperswilJonaId,
        };
    }

    private async Task SeedScheduledMerge(int dayDelta = 0, SharedProto.DomainOfInfluenceCanton canton = SharedProto.DomainOfInfluenceCanton.Sg)
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
                Canton = canton,
            },
            ActiveFrom = MockedClock.GetTimestampDate(dayDelta),
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

        if (dayDelta <= 0)
        {
            await TestEventPublisher.Publish(1, new CountingCirclesMergerActivated
            {
                Merger = mergerEventData,
                EventInfo = GetMockedEventInfo(),
            });
        }
    }
}
