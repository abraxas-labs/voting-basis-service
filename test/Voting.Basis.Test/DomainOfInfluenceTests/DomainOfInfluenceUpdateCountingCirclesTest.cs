// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateCountingCirclesTest : BaseGrpcTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceUpdateCountingCirclesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task AddCountingCircleShouldReturnOk()
    {
        await AdminClient.UpdateCountingCircleEntriesAsync(NewValidRequest(x =>
        {
            x.CountingCircleIds.Add(CountingCircleMockedData.IdStGallen);
            x.CountingCircleIds.Add(CountingCircleMockedData.IdUzwilKirche);
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCountingCircleEntriesUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestProcessorAddCountingCircle()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
                CountingCircleIds =
                    {
                        CountingCircleMockedData.IdStGallen,
                        CountingCircleMockedData.IdUzwilKirche,
                    },
            },
            EventInfo = GetMockedEventInfo(),
        });

        var response = await AdminClient.GetAsync(new GetDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot("response");

        var parentDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidBund)
            .ToListAsync());

        var selfDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidStGallen)
            .ToListAsync());

        var childDoiUzwilKircheCc = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidGossau && doiCc.CountingCircleId == CountingCircleMockedData.UzwilKirche.Id)
            .FirstOrDefaultAsync());

        var parentDoiUzwilKircheCc = parentDoiCcs.First(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.UzwilKirche.Id);
        var selfDoiUzwilKircheCc = selfDoiCcs.First(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.UzwilKirche.Id);

        parentDoiCcs.Should().HaveCount(5);
        selfDoiCcs.Should().HaveCount(4);

        parentDoiUzwilKircheCc.Should().NotBeNull();
        parentDoiUzwilKircheCc.Inherited.Should().BeTrue();

        selfDoiUzwilKircheCc.Should().NotBeNull();
        selfDoiUzwilKircheCc.Inherited.Should().BeFalse();

        childDoiUzwilKircheCc.Should().BeNull();

        // check cc options is created correctly.
        var contestId = Guid.Parse(ContestMockedData.IdStGallenEvoting);
        var options = await RunOnDb(db => db.ContestCountingCircleOptions.Where(x => x.ContestId == contestId).ToListAsync());
        var optionsByCcId = options.ToDictionary(x => x.CountingCircleId);
        optionsByCcId[parentDoiUzwilKircheCc.CountingCircleId].EVoting.Should().BeFalse();
    }

    [Fact]
    public async Task TestProcessorRemoveCountingCircle()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
            },
            EventInfo = GetMockedEventInfo(),
        });

        var response = await AdminClient.GetAsync(new GetDomainOfInfluenceRequest
        {
            Id = DomainOfInfluenceMockedData.IdStGallen,
        });
        response.MatchSnapshot("response");

        var parentDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidBund)
            .ToListAsync());

        var selfDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == DomainOfInfluenceMockedData.GuidStGallen)
            .ToListAsync());

        var parentDoiStGallenCc = parentDoiCcs.Find(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.StGallen.Id);
        var selfDoiStGallenCc = selfDoiCcs.Find(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.StGallen.Id);

        parentDoiCcs.Should().HaveCount(3);
        selfDoiCcs.Should().HaveCount(2);

        parentDoiStGallenCc.Should().BeNull();
        selfDoiStGallenCc.Should().BeNull();
    }

    [Fact]
    public async Task AlreadyExistingInTreeShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.UpdateCountingCircleEntriesAsync(NewValidRequest(x => x.CountingCircleIds.Add(CountingCircleMockedData.IdUzwil))),
            StatusCode.InvalidArgument,
            "A CountingCircle cannot be added twice in the same DomainOfInfluence Tree");
    }

    [Fact]
    public async Task RemoveCountingCircleShouldReturnOk()
    {
        await AdminClient.UpdateCountingCircleEntriesAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<DomainOfInfluenceCountingCircleEntriesUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task InvalidGuid()
        => await AssertStatus(
            async () => await AdminClient.UpdateCountingCircleEntriesAsync(NewValidRequest(x => x.Id = DomainOfInfluenceMockedData.IdInvalid)),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task NonExistingCountingCircleGuid()
        => await AssertStatus(
            async () => await AdminClient.UpdateCountingCircleEntriesAsync(NewValidRequest(x =>
            {
                x.CountingCircleIds.Add(CountingCircleMockedData.IdBund);
                x.CountingCircleIds.Add(CountingCircleMockedData.IdNotExisting);
            })),
            StatusCode.NotFound);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
                .UpdateCountingCircleEntriesAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }

    private UpdateDomainOfInfluenceCountingCircleEntriesRequest NewValidRequest(
        Action<UpdateDomainOfInfluenceCountingCircleEntriesRequest>? customizer = null)
    {
        var request = new UpdateDomainOfInfluenceCountingCircleEntriesRequest
        {
            Id = DomainOfInfluenceMockedData.IdStGallen,
        };

        customizer?.Invoke(request);
        return request;
    }
}
