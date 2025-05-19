// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Domain.Aggregate;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Repositories;
using Voting.Basis.Data.Utils;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Basis.Test.MockedData;

public static class CountingCircleMockedData
{
    public const string IdBund = "eea024ee-7f93-4a58-9e7d-6e6b0c11b723";
    public const string IdStGallen = "1e2e062e-288d-4347-9f22-224e69b95bed";
    public const string IdGossau = "0bb8d89c-d387-40d0-83cf-84e118a8d524";
    public const string IdUzwil = "ca7be031-d4ce-4bb6-9538-b5e5d19e5a3e";
    public const string IdRorschach = "eae2cfaf-c787-48b9-a108-c975b0a580dc";
    public const string IdUzwilKirche = "ca7be031-d4ce-4bb6-9538-b5e5d19e5a4e";
    public const string IdUzwilKircheAndere = "c375f97a-0ba2-4a93-8c76-08a0661d9962";
    public const string IdZurich = "5f6726d6-05e4-4dc3-9283-a48fce74615b";

    public const string IdJona = "3ec3d50c-6cb7-4ba1-bd1a-2882153834b0";
    public const string IdRapperswil = "2a3a799a-2437-47da-86fe-06decf70a267";

    public const string IdNotExisting = "24ba7bab-ad49-44de-b610-999999999999";
    public const string IdInvalid = "22949a1e";

    public const string CountingCircleUzwilKircheSecureConnectId = "kirche-uzwil-010000";

    public static CountingCircle Bund
        => new CountingCircle
        {
            Name = "Bund",
            Id = Guid.Parse(IdBund),
            Bfs = "1",
            Code = "1_code",
            SortNumber = 1,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = "random",
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static CountingCircle StGallen
        => new CountingCircle
        {
            Id = Guid.Parse(IdStGallen),
            Name = "St. Gallen",
            NameForProtocol = "Stadt St. Gallen",
            Bfs = "5500",
            Code = "5500c",
            SortNumber = 9000,
            ResponsibleAuthority = new Authority
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
            ContactPersonDuringEvent = new CountingCircleContactPerson
            {
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-sg",
                FirstName = "Hans-sg",
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson
            {
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 22",
                MobilePhone = "071 123 12 33",
                FamilyName = "Wichtig-sg",
                FirstName = "Rudolph-sg",
            },
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static CountingCircle Gossau
        => new CountingCircle
        {
            Name = "Gossau",
            NameForProtocol = "Stadt Gossau",
            Id = Guid.Parse(IdGossau),
            Bfs = "3443",
            Code = "3443c",
            SortNumber = 9001,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
            Canton = DomainOfInfluenceCanton.Sg,
            ECounting = true,
            EVoting = true,
            EVotingActiveFrom = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

    public static CountingCircle Uzwil
        => new CountingCircle
        {
            Id = Guid.Parse(IdUzwil),
            Name = "Uzwil",
            NameForProtocol = "Stadt Uzwil",
            Bfs = "1234",
            Code = "1234c",
            SortNumber = 92,
            ResponsibleAuthority = new Authority
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
            ContactPersonDuringEvent = new CountingCircleContactPerson
            {
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster",
                FirstName = "Hans",
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson
            {
                Email = "uzwil-test2@abraxas.ch",
                Phone = "071 123 12 22",
                MobilePhone = "071 123 12 33",
                FamilyName = "Wichtig",
                FirstName = "Rudolph",
            },
            Electorates = new List<CountingCircleElectorate>
            {
                new()
                {
                    Id = BasisUuidV5.BuildCountingCircleElectorate(Guid.Parse(IdUzwil), new[] { DomainOfInfluenceType.Sk }),
                    DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Sk },
                },
                new()
                {
                    Id = BasisUuidV5.BuildCountingCircleElectorate(Guid.Parse(IdUzwil), new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct }),
                    DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct },
                },
            },
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static CountingCircle Rorschach
        => new CountingCircle
        {
            Id = Guid.Parse(IdRorschach),
            Name = "Rorschach",
            NameForProtocol = "Stadt Rorschach",
            Bfs = "5600",
            Code = "5600c",
            SortNumber = 9003,
            ResponsibleAuthority = new Authority
            {
                Name = "Rorschach",
                Email = "ro@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseX",
                City = "MyCityX",
                Zip = "9200",
                SecureConnectId = "1234444444",
            },
            ContactPersonSameDuringEventAsAfter = true,
            ContactPersonDuringEvent = new CountingCircleContactPerson
            {
                Email = "ro@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-sg",
                FirstName = "Hans-sg",
            },
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static CountingCircle UzwilKirche
        => new CountingCircle
        {
            Name = "Kirche Uzwil",
            NameForProtocol = "Kirche Uzwil",
            Id = Guid.Parse(IdUzwilKirche),
            Bfs = "none",
            Code = "nonec",
            SortNumber = 21500,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = CountingCircleUzwilKircheSecureConnectId,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static CountingCircle UzwilKircheAndere
        => new CountingCircle
        {
            Name = "Kirche Uzwil Andere",
            NameForProtocol = "Kirche Uzwil Andere",
            Id = Guid.Parse(IdUzwilKircheAndere),
            Bfs = "none",
            Code = "none_uc",
            SortNumber = 21500,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = CountingCircleUzwilKircheSecureConnectId,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static CountingCircle Zurich
        => new CountingCircle
        {
            Id = Guid.Parse(IdZurich),
            Name = "Zürich",
            NameForProtocol = "Zürich",
            Bfs = "1234",
            Code = "1234c",
            SortNumber = 9000,
            ResponsibleAuthority = new Authority
            {
                Name = "Zürich",
                Email = "zh@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseX",
                City = "MyCityX",
                Zip = "9000",
                SecureConnectId = "zürich-sec-id",
            },
            ContactPersonSameDuringEventAsAfter = true,
            ContactPersonDuringEvent = new CountingCircleContactPerson
            {
                Email = "zh@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-zh",
                FirstName = "Hans-zh",
            },
            Canton = DomainOfInfluenceCanton.Zh,
        };

    public static CountingCircle Rapperswil
        => new CountingCircle
        {
            Name = "Rapperswil",
            NameForProtocol = "Stadt Rapperswil",
            Id = Guid.Parse(IdRapperswil),
            Bfs = "none",
            Code = "nonec",
            SortNumber = 9004,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = "random",
            },
            ContactPersonSameDuringEventAsAfter = true,
            ContactPersonAfterEvent = new CountingCircleContactPerson
            {
                Email = "rapperswil@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-sg",
                FirstName = "Hans-sg",
            },
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static CountingCircle Jona
        => new CountingCircle
        {
            Name = "Jona",
            NameForProtocol = "Stadt Jona",
            Id = Guid.Parse(IdJona),
            Bfs = "none",
            Code = "nonec",
            SortNumber = 9005,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = "random",
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson
            {
                Email = "jona@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-sg",
                FirstName = "Hans-sg",
            },
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static IEnumerable<CountingCircle> All
    {
        get
        {
            yield return Bund;
            yield return StGallen;
            yield return Uzwil;
            yield return UzwilKirche;
            yield return UzwilKircheAndere;
            yield return Gossau;
            yield return Jona;
            yield return Rapperswil;
            yield return Zurich;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var ccRepo = sp.GetRequiredService<CountingCircleRepo>();
            foreach (var cc in All)
            {
                await ccRepo.Create(cc, new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            }

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues(string.Empty, "test", "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();
            var countingCirclesAggregates = All.Select(cc => ToAggregate(cc, aggregateFactory, mapper));

            foreach (var countingCircleAggregate in countingCirclesAggregates)
            {
                await aggregateRepository.Save(countingCircleAggregate);
            }

            sp.GetRequiredService<EventPublisherMock>().Clear();
        });
    }

    private static CountingCircleAggregate ToAggregate(
        CountingCircle countingCircle,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<CountingCircleAggregate>();
        countingCircle.CreatedOn = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        countingCircle.ModifiedOn = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var domainCc = mapper.Map<Core.Domain.CountingCircle>(countingCircle);
        aggregate.CreateFrom(domainCc);
        return aggregate;
    }
}
