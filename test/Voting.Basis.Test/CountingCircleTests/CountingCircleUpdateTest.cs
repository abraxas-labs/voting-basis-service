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
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Jobs;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleUpdateTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";

    public CountingCircleUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await CountingCircleMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var electorateChId = Guid.Parse("94ff0364-19e1-4c1d-9179-17b18ad39b72");

        // We need the domain of influences here, otherwise we cannot test the permissions later on
        await DomainOfInfluenceMockedData.Seed(RunScoped, false);

        await TestEventPublisher.Publish(
            new CountingCircleUpdated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Id = CountingCircleMockedData.IdUzwil,
                    Name = "Uzwil-2",
                    Bfs = "1234-2",
                    Code = "Code234",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "Uzwil-2",
                        Email = "uzwil-test2@abraxas.ch",
                        Phone = "072 123 12 20",
                        Street = "WerkstrasseX",
                        City = "MyCityX",
                        Zip = "9200",
                        SecureConnectId = TestDefaults.TenantId,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test-2@abraxas.ch",
                        Phone = "072 123 12 21",
                        MobilePhone = "072 123 12 31",
                        FamilyName = "Muster-2",
                        FirstName = "Hans-2",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test22@abraxas.ch",
                        Phone = "072 123 12 22",
                        MobilePhone = "072 123 12 33",
                        FamilyName = "Wichtig-2",
                        FirstName = "Rudolph-2",
                    },
                    NameForProtocol = "Uzwil updated",
                    SortNumber = 101,
                    Electorates =
                    {
                        new CountingCircleElectorateEventData
                        {
                            Id = electorateChId.ToString(),
                            DomainOfInfluenceTypes = { DomainOfInfluenceType.Ch },
                        },
                    },
                    Canton = DomainOfInfluenceCanton.Sg,
                    ECounting = true,
                },
                EventInfo = GetMockedEventInfo(),
            },
            new CountingCircleUpdated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Id = CountingCircleMockedData.IdStGallen,
                    Name = "St. Gallen-2",
                    Bfs = "55002",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "St. Gallen2",
                        Email = "sg2@abraxas.ch",
                        Phone = "072 123 12 20",
                        Street = "WerkstrasseSG",
                        City = "MyCitysg",
                        Zip = "9000",
                        SecureConnectId = "123444",
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "sg2@abraxas.ch",
                        Phone = "072 123 12 21",
                        MobilePhone = "072 123 12 31",
                        FamilyName = "Muster-sg2",
                        FirstName = "Hans-sg2",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "sg2@abraxas.ch",
                        Phone = "072 123 12 22",
                        MobilePhone = "072 123 12 33",
                        FamilyName = "Wichtig-sg2",
                        FirstName = "Rudolph-sg2",
                    },
                    NameForProtocol = "St. Gallen updated",
                    SortNumber = 102,
                    Canton = DomainOfInfluenceCanton.Sg,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var countingCircles = await AdminClient.ListAsync(new ListCountingCircleRequest());
        countingCircles.MatchSnapshot("countingCircles");

        var electorateExists = await RunOnDb(db => db.CountingCircleElectorates.AnyAsync(e => e.Id == electorateChId));
        electorateExists.Should().BeTrue();

        var permissions = await RunOnDb(db => db.DomainOfInfluencePermissions
            .OrderBy(x => x.TenantId)
            .ThenBy(x => x.DomainOfInfluenceId)
            .ToListAsync());
        foreach (var permission in permissions)
        {
            permission.CountingCircleIds.Sort();
        }

        permissions.MatchSnapshot("permissions", x => x.Id);

        await AssertHasPublishedEventProcessedMessage(CountingCircleUpdated.Descriptor, Guid.Parse(CountingCircleMockedData.IdUzwil));
        await AssertHasPublishedEventProcessedMessage(CountingCircleUpdated.Descriptor, Guid.Parse(CountingCircleMockedData.IdStGallen));
    }

    [Fact]
    public async Task TestAsElectionAdminUpdateAuthoritySecureConnectId()
        => await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequestNoChanges(o =>
                o.ResponsibleAuthority.SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id)),
            StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsElectionAdminUpdateBfs()
        => await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequestNoChanges(o => o.Bfs = "updated")),
            StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsElectionAdminUpdateName()
        => await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequestNoChanges(o => o.Name = "updated")),
            StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsElectionAdminUpdateCode()
        => await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequestNoChanges(o => o.Code = "updated")),
            StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsElectionAdminOtherTenant()
        => await AssertStatus(
            async () => await ElectionAdminClient.UpdateAsync(
                NewValidRequestStGallenNoChanges(o => o.ContactPersonAfterEvent.FamilyName += "-updated")),
            StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsElectionAdminOtherElectorate()
    => await AssertStatus(
        async () => await ElectionAdminClient.UpdateAsync(
            NewValidRequestStGallenNoChanges(o =>
                o.Electorates.Add(new ProtoModels.CountingCircleElectorate { DomainOfInfluenceTypes = { DomainOfInfluenceType.Sc } }))),
        StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsElectionAdminDifferentCanton()
    => await AssertStatus(
        async () => await ElectionAdminClient.UpdateAsync(
            NewValidRequestStGallenNoChanges(o =>
                o.Canton = DomainOfInfluenceCanton.Gr)),
        StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsCantonAdminDifferentCanton()
    => await AssertStatus(
        async () => await CantonAdminClient.UpdateAsync(
            NewValidRequestStGallenNoChanges(o =>
                o.Canton = DomainOfInfluenceCanton.Gr)),
        StatusCode.PermissionDenied);

    [Fact]
    public async Task TestAsElectionAdmin()
    {
        await ElectionAdminClient.UpdateAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestAsAdmin()
    {
        await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
        {
            o.Name = "updated-1";
            o.Bfs = "update2";
            o.ResponsibleAuthority.Name = "this should get updated automatically";
            o.ResponsibleAuthority.SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id;
            o.NameForProtocol = "Stadt Uzwil updated";
            o.SortNumber = 95;
            o.Electorates.Add(new ProtoModels.CountingCircleElectorate { DomainOfInfluenceTypes = { DomainOfInfluenceType.Bz } });
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        eventData.CountingCircle.Electorates.Last().Id.Should().NotBeEmpty();
        eventData.CountingCircle.Electorates.Last().Id = string.Empty;
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task MissingEVotingActiveFromShouldSetEVotingFalse()
    {
        await CantonAdminClient.UpdateAsync(NewValidRequest(
            x => x.EVotingActiveFrom = null));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        eventData.CountingCircle.EVoting.Should().BeFalse();
    }

    [Fact]
    public async Task EVotingActiveFromInFutureShouldSetEVotingFalse()
    {
        await CantonAdminClient.UpdateAsync(NewValidRequest(
            x => x.EVotingActiveFrom = MockedClock.GetTimestampDate(1)));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        eventData.CountingCircle.EVoting.Should().BeFalse();
    }

    [Fact]
    public async Task EVotingActiveFromInPastShouldSetEVotingTrue()
    {
        await CantonAdminClient.UpdateAsync(NewValidRequest(
            x => x.EVotingActiveFrom = MockedClock.GetTimestampDate(-1)));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        eventData.CountingCircle.EVoting.Should().BeTrue();
    }

    [Fact]
    public Task InvalidSecureConnectTenant()
        => AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
                o.ResponsibleAuthority.SecureConnectId = "123333333333333333")),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task ElectorateWithDuplicateDoiTypesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
                o.Electorates.Add(new ProtoModels.CountingCircleElectorate
                {
                    DomainOfInfluenceTypes = { DomainOfInfluenceType.Sk },
                }))),
            StatusCode.InvalidArgument,
            "A domain of influence type in an electorate must be unique per counting circle");
    }

    [Fact]
    public async Task ElectorateWithNoDoiTypeShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
                o.Electorates.Add(new ProtoModels.CountingCircleElectorate()))),
            StatusCode.InvalidArgument,
            "Cannot create an electorate without a domain of influence type");
    }

    [Fact]
    public Task NotFound()
        => AssertStatus(
            async () => await CantonAdminClient.UpdateAsync(
                NewValidRequest(o => o.Id = IdNotFound)),
            StatusCode.NotFound);

    [Fact]
    public async Task JobShouldActivateEVoting()
    {
        var ccId = Guid.Parse(CountingCircleMockedData.IdUzwil);

        await SeedEVotingActivateFrom(ccId);
        var countingCircle = await RunOnDb(db => db.CountingCircles.SingleAsync(cc => cc.Id == ccId));
        countingCircle.EVoting.Should().BeFalse();
        countingCircle.EVotingActiveFrom.Should().NotBeNull();

        AdjustableMockedClock.OverrideUtcNow = MockedClock.GetDate(3);
        await RunScoped<ActivateCountingCircleEVotingJob>(job => job.Run(CancellationToken.None));
        AdjustableMockedClock.OverrideUtcNow = null;

        var ccUpdatedEvent = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        ccUpdatedEvent.MatchSnapshot("ccUpdated");

        await TestEventPublisher.Publish(1, ccUpdatedEvent);

        countingCircle = await RunOnDb(db => db.CountingCircles.SingleAsync(cc => cc.Id == ccId));
        countingCircle.EVoting.Should().BeTrue();
        countingCircle.EVotingActiveFrom.Should().NotBeNull();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private async Task SeedEVotingActivateFrom(Guid countingCircleId)
    {
        await CantonAdminClient.UpdateAsync(NewValidRequest(o =>
        {
            o.Id = countingCircleId.ToString();
            o.EVotingActiveFrom = MockedClock.GetTimestampDate(3);
            o.ContactPersonAfterEvent.FirstName = "EVoting";
            o.ContactPersonDuringEvent.FirstName = "EVoting";
            o.Electorates.Add(new ProtoModels.CountingCircleElectorate
            {
                DomainOfInfluenceTypes = { DomainOfInfluenceType.An },
            });
        }));

        var ev = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        EventPublisherMock.Clear();
        await TestEventPublisher.Publish(ev);
    }

    private UpdateCountingCircleRequest NewValidRequest(
        Action<UpdateCountingCircleRequest>? customizer = null)
    {
        var request = new UpdateCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdUzwil,
            Name = "Uzwil",
            Bfs = "1234",
            Code = "1234c",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                Name = "Uzwil-updated",
                Email = "uzwil-test2@abraxas.ch",
                Phone = "072 123 12 20",
                Street = "WerkstrasseX2",
                City = "MyCityX2",
                Zip = "9002",
                SecureConnectId = TestDefaults.TenantId,
            },
            ContactPersonSameDuringEventAsAfter = false,
            ContactPersonDuringEvent = new ProtoModels.ContactPerson
            {
                Email = "uzwil-test2@abraxas.ch",
                Phone = "072 123 12 21",
                MobilePhone = "072 123 12 31",
                FamilyName = "Muster2",
                FirstName = "Hans2",
            },
            ContactPersonAfterEvent = new ProtoModels.ContactPerson
            {
                Email = "uzwil-test22@abraxas.ch",
                Phone = "072 123 12 22",
                MobilePhone = "072 123 12 33",
                FamilyName = "Wichtig2",
                FirstName = "Rudolph2",
            },
            NameForProtocol = "Stadt Uzwil",
            SortNumber = 92,
            Electorates =
            {
                new ProtoModels.CountingCircleElectorate
                {
                    DomainOfInfluenceTypes = { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct },
                },
                new ProtoModels.CountingCircleElectorate
                {
                    DomainOfInfluenceTypes = { DomainOfInfluenceType.Sk },
                },
            },
            Canton = DomainOfInfluenceCanton.Sg,
        };
        customizer?.Invoke(request);
        return request;
    }

    private UpdateCountingCircleRequest NewValidRequestNoChanges(
        Action<UpdateCountingCircleRequest>? customizer = null)
    {
        var request = new UpdateCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdUzwil,
            Name = "Uzwil",
            Bfs = "1234",
            Code = "1234c",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                Name = "Uzwil",
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "Werkstrasse",
                City = "MyCity",
                Zip = "9595",
                SecureConnectId = TestDefaults.TenantId,
            },
            ContactPersonSameDuringEventAsAfter = false,
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
            NameForProtocol = "Stadt Uzwil",
            SortNumber = 92,
            Canton = DomainOfInfluenceCanton.Sg,
        };
        customizer?.Invoke(request);
        return request;
    }

    private UpdateCountingCircleRequest NewValidRequestStGallenNoChanges(
        Action<UpdateCountingCircleRequest>? customizer = null)
    {
        var request = new UpdateCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdStGallen,
            Name = "St. Gallen",
            Bfs = "5500",
            Code = "5500c",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                Name = "St. Gallen",
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseX",
                City = "MyCityX",
                Zip = "9000",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            },
            ContactPersonSameDuringEventAsAfter = false,
            ContactPersonDuringEvent = new ProtoModels.ContactPerson
            {
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-sg",
                FirstName = "Hans-sg",
            },
            ContactPersonAfterEvent = new ProtoModels.ContactPerson
            {
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 22",
                MobilePhone = "071 123 12 33",
                FamilyName = "Wichtig-sg",
                FirstName = "Rudolph-sg",
            },
            NameForProtocol = "Stadt St. Gallen",
            SortNumber = 90,
            Canton = DomainOfInfluenceCanton.Sg,
            ECounting = true,
        };
        customizer?.Invoke(request);
        return request;
    }
}
