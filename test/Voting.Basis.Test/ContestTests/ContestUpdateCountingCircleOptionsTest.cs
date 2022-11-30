// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
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

namespace Voting.Basis.Test.ContestTests;

public class ContestUpdateCountingCircleOptionsTest : BaseGrpcTest<ContestService.ContestServiceClient>
{
    public ContestUpdateCountingCircleOptionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldWork()
    {
        var id = ContestMockedData.IdStGallenEvoting;
        await AdminClient.UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
        {
            Id = id,
            Options =
                {
                    new UpdateCountingCircleOptionRequest
                    {
                        CountingCircleId = CountingCircleMockedData.IdGossau,
                        EVoting = false,
                    },
                    new UpdateCountingCircleOptionRequest
                    {
                        CountingCircleId = CountingCircleMockedData.IdUzwil,
                        EVoting = true,
                    },
                },
        });

        var (eventData, eventMetadata) = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleOptionsUpdated, EventSignatureBusinessMetadata>();
        eventData.MatchSnapshot();
        eventMetadata!.ContestId.Should().Be(id);
    }

    [Fact]
    public async Task TestShouldTriggerEventSignatureAndSignEvent()
    {
        await ShouldTriggerEventSignatureAndSignEvent(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await AdminClient.UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
            {
                Id = ContestMockedData.IdStGallenEvoting,
                Options =
                {
                    new UpdateCountingCircleOptionRequest
                    {
                        CountingCircleId = CountingCircleMockedData.IdGossau,
                        EVoting = false,
                    },
                    new UpdateCountingCircleOptionRequest
                    {
                        CountingCircleId = CountingCircleMockedData.IdUzwil,
                        EVoting = true,
                    },
                },
            });

            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCountingCircleOptionsUpdated>();
        });
    }

    [Fact]
    public Task ShouldThrowIfContestNotEVoting()
    {
        return AssertStatus(
            async () => await AdminClient.UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
            {
                Id = ContestMockedData.IdGossau,
                Options =
                {
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdGossau,
                            EVoting = false,
                        },
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdUzwil,
                            EVoting = true,
                        },
                },
            }),
            StatusCode.InvalidArgument,
            "eVoting");
    }

    [Fact]
    public Task ShouldThrowIfDuplicates()
    {
        return AssertStatus(
            async () => await AdminClient.UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
            {
                Id = ContestMockedData.IdStGallenEvoting,
                Options =
                {
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdGossau,
                            EVoting = false,
                        },
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdUzwil,
                            EVoting = true,
                        },
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdUzwil,
                            EVoting = true,
                        },
                },
            }),
            StatusCode.InvalidArgument,
            "exactly once");
    }

    [Fact]
    public Task ShouldThrowIfNotTenant()
    {
        return AssertStatus(
            async () => await AdminClient.UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
            {
                Id = ContestMockedData.IdBundContest,
                Options =
                {
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdGossau,
                            EVoting = false,
                        },
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdUzwil,
                            EVoting = true,
                        },
                },
            }),
            StatusCode.InvalidArgument,
            "does not belong to this tenant");
    }

    [Fact]
    public async Task ShouldWorkAfterTestingPhase()
    {
        var client = new ContestService.ContestServiceClient(
            CreateGrpcChannel(
                tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
                roles: Roles.Admin));

        await client.UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
        {
            Id = ContestMockedData.IdPastUnlockedContest,
            Options =
                {
                    new UpdateCountingCircleOptionRequest
                    {
                        CountingCircleId = CountingCircleMockedData.IdGossau,
                        EVoting = false,
                    },
                    new UpdateCountingCircleOptionRequest
                    {
                        CountingCircleId = CountingCircleMockedData.IdUzwil,
                        EVoting = true,
                    },
                },
        });
    }

    [Fact]
    public async Task ShouldThrowIfLocked()
    {
        var client = new ContestService.ContestServiceClient(
            CreateGrpcChannel(
                tenant: DomainOfInfluenceMockedData.Bund.SecureConnectId,
                roles: Roles.Admin));

        await AssertStatus(
            async () => await client.UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
            {
                Id = ContestMockedData.IdPastLockedContest,
                Options =
                {
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdGossau,
                            EVoting = false,
                        },
                        new UpdateCountingCircleOptionRequest
                        {
                            CountingCircleId = CountingCircleMockedData.IdUzwil,
                            EVoting = true,
                        },
                },
            }),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestAggregate()
    {
        var id = ContestMockedData.IdStGallenEvoting;
        var guid = Guid.Parse(id);
        await TestEventPublisher.Publish(new ContestCountingCircleOptionsUpdated
        {
            ContestId = id,
            Options =
                {
                    new ContestCountingCircleOptionEventData
                    {
                        CountingCircleId = CountingCircleMockedData.IdGossau,
                        EVoting = false,
                    },
                    new ContestCountingCircleOptionEventData
                    {
                        CountingCircleId = CountingCircleMockedData.IdUzwil,
                        EVoting = true,
                    },
                },
        });

        var options = await RunOnDb(db => db.ContestCountingCircleOptions
            .Where(x => x.ContestId == guid)
            .ToListAsync());
        var byCcId = options.ToDictionary(x => x.CountingCircleId.ToString());
        byCcId[CountingCircleMockedData.IdGossau].EVoting.Should().BeFalse();
        byCcId[CountingCircleMockedData.IdUzwil].EVoting.Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .UpdateCountingCircleOptionsAsync(new UpdateCountingCircleOptionsRequest
            {
                Id = ContestMockedData.IdGossau,
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
