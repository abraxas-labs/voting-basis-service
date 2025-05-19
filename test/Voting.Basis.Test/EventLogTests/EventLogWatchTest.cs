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
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Messaging;
using Voting.Basis.Core.Services.Read;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.EventLogTests;

public class EventLogWatchTest : BaseGrpcTest<EventLogService.EventLogServiceClient>
{
    public EventLogWatchTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public void EventFilterTest()
    {
        var contestId = Guid.Parse("25220c98-0a39-423a-b426-b6d9ebb97d97");
        var filter = new EventLogReader.EventFilter("364d0bf2-e689-4b38-9e75-a5e2f230d952", new HashSet<string> { ContestCreated.Descriptor.FullName }, contestId);
        filter.Filter(new EventProcessedMessage(ContestCreated.Descriptor.FullName, string.Empty, Guid.NewGuid(), null, contestId, null, null, null))
            .Should()
            .BeTrue();

        // missing contest id
        filter.Filter(new EventProcessedMessage(ContestCreated.Descriptor.FullName, string.Empty, Guid.NewGuid(), null, null, null, null, null))
            .Should()
            .BeFalse();

        // mismatched contest id
        filter.Filter(new EventProcessedMessage(ContestCreated.Descriptor.FullName, string.Empty, Guid.NewGuid(), null, Guid.Parse("a25ff820-cdbe-458b-8b0a-f86fa2315550"), null, null, null))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void EventFilterTestNoContestId()
    {
        var filter = new EventLogReader.EventFilter("364d0bf2-e689-4b38-9e75-a5e2f230d952", new HashSet<string> { ContestCreated.Descriptor.FullName }, null);
        filter.Filter(new EventProcessedMessage(ContestCreated.Descriptor.FullName, string.Empty, null, null, Guid.NewGuid(), null, null, null))
            .Should()
            .BeTrue();

        // missing contest id
        filter.Filter(new EventProcessedMessage(ContestCreated.Descriptor.FullName, string.Empty, Guid.NewGuid(), null, null, null, null, null))
            .Should()
            .BeTrue();

        // another contest id
        filter.Filter(new EventProcessedMessage(ContestCreated.Descriptor.FullName, string.Empty, Guid.NewGuid(), null, Guid.NewGuid(), null, null, null))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task ShouldNotProcessEventsFromInaccessibleDoi()
    {
        var filter1 = new WatchEventsRequestFilter
        {
            Id = "99505583-3c10-4d0d-8feb-4c96cac93499",
            Types_ =
            {
                ContestDeleted.Descriptor.FullName,
            },
        };
        var filter2 = new WatchEventsRequestFilter
        {
            Id = "13e11f83-e3b4-4c98-85a6-9500e5363373",
            Types_ =
            {
                ContestCreated.Descriptor.FullName,
            },
        };

        var eventData1 = new ContestCreated
        {
            Contest = new ContestEventData
            {
                Id = "12dee128-5ca1-466d-a514-7c74d262efa2",
                Date = new DateTime(2019, 1, 3, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdKirchgemeinde,
                EndOfTestingPhase = new DateTime(2019, 1, 1, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
            },
        };
        var eventData2 = new ContestCreated
        {
            Contest = new ContestEventData
            {
                Id = "fe1cb244-cd1a-47e7-b0e9-7973581dc659",
                Date = new DateTime(2019, 1, 3, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2019, 1, 1, 12, 45, 0, DateTimeKind.Utc).ToTimestamp(),
            },
        };

        var data = await PublishAndWatchEvents([filter1, filter2], [eventData1, eventData2], 1);
        data.Should().HaveCount(1);
        data[0].EntityId.Should().Be(eventData2.Contest.Id);
        data[0].ContestId.Should().Be(eventData2.Contest.Id);
        data[0].FilterId.Should().Be(filter2.Id);
    }

    [Fact]
    public async Task ShouldNotProcessEventsFromInaccessibleCountingCircle()
    {
        var filter = new WatchEventsRequestFilter
        {
            Id = "99505583-3c10-4d0d-8feb-4c96cac93499",
            Types_ =
            {
                CountingCircleCreated.Descriptor.FullName,
            },
        };

        var eventData1 = new CountingCircleCreated
        {
            CountingCircle = new CountingCircleEventData
            {
                Name = "genf",
                Bfs = "1234",
                Code = "Code1234",
                Id = "e4f39f70-790c-4b3a-85c3-c19d98c7228d",
                ResponsibleAuthority = new AuthorityEventData
                {
                    Name = "genf",
                    Email = "genf-test@abraxas.ch",
                    Phone = "071 123 12 20",
                    Street = "WerkstrasseX",
                    City = "MyCityX",
                    Zip = "9200",
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantGenf.Id,
                },
            },
            EventInfo = GetMockedEventInfo(),
        };
        eventData1.EventInfo.Tenant.Id = SecureConnectTestDefaults.MockedTenantGenf.Id;

        var eventData2 = new CountingCircleCreated
        {
            CountingCircle = new CountingCircleEventData
            {
                Name = "Uzwil",
                Bfs = "1234",
                Code = "Code1234",
                Id = "473eebfb-1479-4d95-9ce2-4afffe2a0972",
                ResponsibleAuthority = new AuthorityEventData
                {
                    Name = "Uzwil",
                    Email = "uzwil-test@abraxas.ch",
                    Phone = "071 123 12 20",
                    Street = "WerkstrasseX",
                    City = "MyCityX",
                    Zip = "9200",
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantGenf.Id,
                },
            },
        };

        var data = await PublishAndWatchEvents([filter], [eventData1, eventData2], 1);
        data.Should().HaveCount(1);
        data[0].EntityId.Should().Be(eventData2.CountingCircle.Id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new EventLogService.EventLogServiceClient(channel).Watch(
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
        yield return Roles.CantonAdminReadOnly;
        yield return Roles.ElectionAdminReadOnly;
    }

    private async Task<List<Event>> PublishAndWatchEvents<TEvent>(
        IEnumerable<WatchEventsRequestFilter> filters,
        IEnumerable<TEvent> events,
        int expectedReceivedCount)
        where TEvent : IMessage<TEvent>
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var responseStream = ElectionAdminClient.Watch(
            new()
            {
                Filters =
                {
                    filters,
                },
            },
            new(cancellationToken: cts.Token));

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);

        await TestEventPublisher.Publish(events.ToArray());
        return await responseStream.ResponseStream.ReadNIgnoreCancellation(expectedReceivedCount, cts.Token);
    }
}
