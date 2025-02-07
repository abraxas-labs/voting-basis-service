// (c) Copyright by Abraxas Informatik AG
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
using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
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

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task Test()
    {
        var response = await CantonAdminClient.CreateAsync(NewValidRequest());

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

        var countingCircleId1 = Guid.Parse("eae2cfaf-c787-48b9-a108-c975b0a580da");
        var countingCircleId2 = Guid.Parse("eae2cfaf-c787-48b9-a108-c975b0a580db");

        await TestEventPublisher.Publish(
            new CountingCircleCreated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Name = "Uzwil",
                    Bfs = "1234",
                    Code = "Code1234",
                    Id = countingCircleId1.ToString(),
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
                    Id = countingCircleId2.ToString(),
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
                    Canton = DomainOfInfluenceCanton.Sg,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var countingCircles = await AdminClient.ListAsync(new ListCountingCircleRequest());
        countingCircles.MatchSnapshot();

        var electorateExists = await RunOnDb(db => db.CountingCircleElectorates.AnyAsync(e => e.Id == electorateBzId));
        electorateExists.Should().BeTrue();

        await AssertHasPublishedMessage<CountingCircleChangeMessage>(
            x => x.CountingCircle.HasEqualIdAndNewEntityState(countingCircleId1, EntityState.Added));
        await AssertHasPublishedMessage<CountingCircleChangeMessage>(
            x => x.CountingCircle.HasEqualIdAndNewEntityState(countingCircleId2, EntityState.Added));
    }

    [Fact]
    public Task InvalidSecureConnectTenant()
        => AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
                o.ResponsibleAuthority.SecureConnectId = "123333333333333333")),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task ElectorateWithDuplicateDoiTypesShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
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
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(o =>
                o.Electorates.Add(new ProtoModels.CountingCircleElectorate()))),
            StatusCode.InvalidArgument,
            "Cannot create an electorate without a domain of influence type");
    }

    [Fact]
    public async Task TestCantonAdminOtherCantonShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.CreateAsync(NewValidRequest(req => req.Canton = DomainOfInfluenceCanton.Zh)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task MissingEVotingActiveFromShouldSetEVotingFalse()
    {
        await CantonAdminClient.CreateAsync(NewValidRequest(
            x => x.EVotingActiveFrom = null));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleCreated>();
        eventData.CountingCircle.EVoting.Should().BeFalse();
    }

    [Fact]
    public async Task EVotingActiveFromInFutureShouldSetEVotingFalse()
    {
        await CantonAdminClient.CreateAsync(NewValidRequest(
            x => x.EVotingActiveFrom = MockedClock.GetTimestampDate(1)));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleCreated>();
        eventData.CountingCircle.EVoting.Should().BeFalse();
    }

    [Fact]
    public async Task EVotingActiveFromInPastShouldSetEVotingTrue()
    {
        await CantonAdminClient.CreateAsync(NewValidRequest(
            x => x.EVotingActiveFrom = MockedClock.GetTimestampDate(-1)));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleCreated>();
        eventData.CountingCircle.EVoting.Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CountingCircleService.CountingCircleServiceClient(channel)
            .CreateAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
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
            Canton = DomainOfInfluenceCanton.Sg,
        };
        customizer?.Invoke(request);
        return request;
    }
}
