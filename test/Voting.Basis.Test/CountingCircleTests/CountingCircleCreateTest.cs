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
using Voting.Basis.Core.Auth;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleCreateTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    public CountingCircleCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Test()
    {
        var response = await AdminClient.CreateAsync(NewValidRequest());

        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleCreated>();

        eventData.CountingCircle.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.CountingCircle.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(
            new CountingCircleCreated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Name = "Uzwil",
                    Bfs = "1234",
                    Code = "Code1234",
                    Id = "eae2cfaf-c787-48b9-a108-c975b0a580da",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "Uzwil",
                        Email = "uzwil-test@abraxas.ch",
                        Phone = "071 123 12 20",
                        Street = "WerkstrasseX",
                        City = "MyCityX",
                        Zip = "9200",
                        SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test@abraxas.ch",
                        Phone = "071 123 12 21",
                        MobilePhone = "071 123 12 31",
                        FamilyName = "Muster",
                        FirstName = "Hans",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "uzwil-test2@abraxas.ch",
                        Phone = "071 123 12 22",
                        MobilePhone = "071 123 12 33",
                        FamilyName = "Wichtig",
                        FirstName = "Rudolph",
                    },
                    NameForProtocol = "Stadt Uzwil",
                    SortNumber = 1,
                },
                EventInfo = GetMockedEventInfo(),
            },
            new CountingCircleCreated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Name = "St. Gallen",
                    Bfs = "5500",
                    Id = "eae2cfaf-c787-48b9-a108-c975b0a580db",
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        Name = "St. Gallen",
                        Email = "sg@abraxas.ch",
                        Phone = "071 123 12 20",
                        Street = "WerkstrasseSG",
                        City = "MyCitySG",
                        Zip = "9000",
                        SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    },
                    ContactPersonSameDuringEventAsAfter = false,
                    ContactPersonDuringEvent = new ContactPersonEventData
                    {
                        Email = "sg@abraxas.ch",
                        Phone = "071 123 12 21",
                        MobilePhone = "071 123 12 31",
                        FamilyName = "Muster-sg",
                        FirstName = "Hans-sg",
                    },
                    ContactPersonAfterEvent = new ContactPersonEventData
                    {
                        Email = "sg@abraxas.ch",
                        Phone = "071 123 12 22",
                        MobilePhone = "071 123 12 33",
                        FamilyName = "Wichtig-sg",
                        FirstName = "Rudolph-sg",
                    },
                    NameForProtocol = "Stadt St. Gallen",
                    SortNumber = 1,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var countingCircles = await AdminClient.ListAsync(new ListCountingCircleRequest());
        countingCircles.CountingCircles_.Should().HaveCount(2);
        countingCircles.MatchSnapshot();
    }

    [Fact]
    public Task InvalidSecureConnectTenant()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
                o.ResponsibleAuthority.SecureConnectId = "123333333333333333")),
            StatusCode.InvalidArgument);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .CreateAsync(NewValidRequest());

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return Roles.ElectionAdmin;
    }

    private CreateCountingCircleRequest NewValidRequest(
        Action<CreateCountingCircleRequest>? customizer = null)
    {
        var request = new CreateCountingCircleRequest
        {
            Name = "Uzwil",
            Bfs = "1234",
            ResponsibleAuthority = new ProtoModels.Authority
            {
                Name = "Uzwil",
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseUZ",
                City = "MyCityUZ",
                Zip = "9200",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
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
            SortNumber = 210,
        };
        customizer?.Invoke(request);
        return request;
    }
}
