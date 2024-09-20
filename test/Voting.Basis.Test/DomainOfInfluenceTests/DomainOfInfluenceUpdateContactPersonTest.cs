// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateContactPersonTest : BaseTest
{
    public DomainOfInfluenceUpdateContactPersonTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(new DomainOfInfluenceContactPersonUpdated
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
            ContactPerson = new ContactPersonEventData
            {
                Email = "hans@muster.com",
                Phone = "071 123 12 12",
                FamilyName = "muster",
                FirstName = "hans",
                MobilePhone = "079 721 21 21",
            },
            EventInfo = GetMockedEventInfo(),
        });

        var doi = await GetDbEntity<DomainOfInfluence>(x => x.Id == DomainOfInfluenceMockedData.GuidGossau);
        var protoDoi = RunScoped<TestMapper, ProtoModels.DomainOfInfluence>(mapper => mapper.Map<ProtoModels.DomainOfInfluence>(doi));
        protoDoi.ContactPerson.MatchSnapshot();
    }
}
