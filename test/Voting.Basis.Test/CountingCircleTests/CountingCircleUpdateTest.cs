// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleUpdateTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

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
                },
                EventInfo = GetMockedEventInfo(),
            });
        var countingCircles = await AdminClient.ListAsync(new ListCountingCircleRequest());
        countingCircles.CountingCircles_.Should().HaveCount(8);
        countingCircles.MatchSnapshot();
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
            async () => await ElectionAdminClient.UpdateAsync(NewValidRequestStGallenNoChanges(o => o.ContactPersonAfterEvent.FamilyName += "-updated")),
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
        await AdminClient.UpdateAsync(NewValidRequest(o =>
        {
            o.Name = "updated-1";
            o.Bfs = "update2";
            o.ResponsibleAuthority.Name = "this should get updated automatically";
            o.ResponsibleAuthority.SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id;
            o.NameForProtocol = "Stadt Uzwil updated";
            o.SortNumber = 95;
        }));

        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleUpdated>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public Task InvalidSecureConnectTenant()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(NewValidRequest(o =>
                o.ResponsibleAuthority.SecureConnectId = "123333333333333333")),
            StatusCode.InvalidArgument);

    [Fact]
    public Task NotFound()
        => AssertStatus(
            async () => await AdminClient.UpdateAsync(
                NewValidRequest(o => o.Id = IdNotFound)),
            StatusCode.NotFound);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .UpdateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
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
        };
        customizer?.Invoke(request);
        return request;
    }
}
