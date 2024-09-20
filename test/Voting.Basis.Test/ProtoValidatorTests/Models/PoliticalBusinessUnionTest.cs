// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class PoliticalBusinessUnionTest : ProtoValidatorBaseTest<PoliticalBusinessUnion>
{
    public static PoliticalBusinessUnion NewValid(Action<PoliticalBusinessUnion>? action = null)
    {
        var politicalBusiness = new PoliticalBusinessUnion
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ContestId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = "description",
            SecureConnectId = "380590188826699143",
            Type = PoliticalBusinessUnionType.PoliticalBusinessUnionMajorityElection,
        };

        action?.Invoke(politicalBusiness);
        return politicalBusiness;
    }

    protected override IEnumerable<PoliticalBusinessUnion> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(1));
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(50));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(18));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(20));
    }

    protected override IEnumerable<PoliticalBusinessUnion> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.ContestId = "invalid-guid");
        yield return NewValid(x => x.ContestId = string.Empty);
        yield return NewValid(x => x.Description = string.Empty);
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(51));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(17));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateNumeric(21));
        yield return NewValid(x => x.SecureConnectId = RandomStringUtil.GenerateAlphabetic(18));
        yield return NewValid(x => x.Type = PoliticalBusinessUnionType.PoliticalBusinessUnionUnspecified);
        yield return NewValid(x => x.Type = (PoliticalBusinessUnionType)10);
    }
}
