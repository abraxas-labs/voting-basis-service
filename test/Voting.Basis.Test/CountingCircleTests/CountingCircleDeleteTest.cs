// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.CountingCircleTests;

public class CountingCircleDeleteTest : BaseGrpcTest<CountingCircleService.CountingCircleServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";
    private string? _authTestCcId;

    public CountingCircleDeleteTest(TestApplicationFactory factory)
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
    public async Task TestInvalidGuid()
        => await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new DeleteCountingCircleRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);

    [Fact]
    public async Task TestNotFound()
        => await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new DeleteCountingCircleRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);

    [Fact]
    public async Task Test()
    {
        await CantonAdminClient.DeleteAsync(new DeleteCountingCircleRequest
        {
            Id = CountingCircleMockedData.IdUzwil,
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<CountingCircleDeleted>();

        eventData.CountingCircleId.Should().Be(CountingCircleMockedData.IdUzwil);
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestCantonAdminOtherCantonShouldThrow()
    {
        await AssertStatus(
            async () => await CantonAdminClient.DeleteAsync(new DeleteCountingCircleRequest
            {
                Id = CountingCircleMockedData.IdZurich,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestAggregate()
    {
        var idUzwil = CountingCircleMockedData.IdUzwil;
        await TestEventPublisher.Publish(new CountingCircleDeleted
        {
            CountingCircleId = idUzwil,
            EventInfo = GetMockedEventInfo(),
        });

        var idGuid = Guid.Parse(idUzwil);
        (await RunOnDb(db => db.CountingCircles.CountAsync(cc => cc.Id == idGuid)))
            .Should().Be(0);

        // These should be updated, the counting circle should be removed everywhere
        // Note that this can have secondary effects, such as a tenant not having access to a domain of influence anymore
        var permissions = await RunOnDb(db => db.DomainOfInfluencePermissions
            .OrderBy(x => x.TenantId)
            .ThenBy(x => x.DomainOfInfluenceId)
            .ToListAsync());
        foreach (var permission in permissions)
        {
            permission.CountingCircleIds.Sort();
        }

        permissions.MatchSnapshot("permissions", x => x.Id);

        await AssertHasPublishedEventProcessedMessage(CountingCircleDeleted.Descriptor, idGuid);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_authTestCcId == null)
        {
            var response = await CantonAdminClient.CreateAsync(new CreateCountingCircleRequest
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
            });

            _authTestCcId = response.Id;
        }

        await new CountingCircleService.CountingCircleServiceClient(channel)
            .DeleteAsync(new DeleteCountingCircleRequest { Id = _authTestCcId });
        _authTestCcId = null;
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
    }
}
