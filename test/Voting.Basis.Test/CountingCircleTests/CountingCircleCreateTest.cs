// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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

        foreach (var electorate in eventData.CountingCircle.Electorates)
        {
            electorate.Id.Should().NotBeEmpty();
            electorate.Id = string.Empty;
        }

        eventData.CountingCircle.Id.Should().Be(response.Id);
        eventData.MatchSnapshot("event", d => d.CountingCircle.Id);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var electorateBzId = Guid.Parse("fbf2386a-4354-46d8-a393-6b2ec3e475b4");

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
                    Electorates =
                    {
                        new CountingCircleElectorateEventData
                        {
                            Id = electorateBzId.ToString(),
                            DomainOfInfluenceTypes = { DomainOfInfluenceType.Bz },
                        },
                        new CountingCircleElectorateEventData
                        {
                            Id = "403ae820-2ad3-482a-a321-3da9745adb1e",
                            DomainOfInfluenceTypes = { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct },
                        },
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

        var electorateExists = await RunOnDb(db => db.CountingCircleElectorates.AnyAsync(e => e.Id == electorateBzId));
        electorateExists.Should().BeTrue();
    }

    [Fact]
    public Task InvalidSecureConnectTenant()
        => AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
                o.ResponsibleAuthority.SecureConnectId = "123333333333333333")),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task ElectorateWithDuplicateDoiTypesShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
                o.Electorates.Add(new ProtoModels.CountingCircleElectorate
                {
                    DomainOfInfluenceTypes =
                    {
                        DomainOfInfluenceType.Ch,
                    },
                }))),
            StatusCode.InvalidArgument,
            "A domain of influence type in an electorate must be unique per counting circle");
    }

    [Fact]
    public async Task ElectorateWithNoDoiTypeShouldThrow()
    {
        await AssertStatus(
            async () => await AdminClient.CreateAsync(NewValidRequest(o =>
                o.Electorates.Add(new ProtoModels.CountingCircleElectorate()))),
            StatusCode.InvalidArgument,
            "Cannot create an electorate without a domain of influence type");
    }

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
            Electorates =
            {
                new ProtoModels.CountingCircleElectorate()
                {
                    DomainOfInfluenceTypes = { DomainOfInfluenceType.Bz },
                },
                new ProtoModels.CountingCircleElectorate()
                {
                    DomainOfInfluenceTypes = { DomainOfInfluenceType.Ct, DomainOfInfluenceType.Ch },
                },
            },
        };
        customizer?.Invoke(request);
        return request;
    }
}
