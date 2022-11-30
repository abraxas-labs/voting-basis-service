// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Basis.Test.ProtoValidatorTests.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class CreateMajorityElectionBallotGroupRequestTest : ProtoValidatorBaseTest<CreateMajorityElectionBallotGroupRequest>
{
    protected override IEnumerable<CreateMajorityElectionBallotGroupRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ShortDescription = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.ShortDescription = RandomStringUtil.GenerateComplexSingleLineText(100));
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateComplexSingleLineText(1));
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateComplexSingleLineText(500));
        yield return NewValidRequest(x => x.Position = 1);
        yield return NewValidRequest(x => x.Position = 100);
        yield return NewValidRequest(x => x.Entries.Clear());
    }

    protected override IEnumerable<CreateMajorityElectionBallotGroupRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
        yield return NewValidRequest(x => x.ShortDescription = string.Empty);
        yield return NewValidRequest(x => x.ShortDescription = RandomStringUtil.GenerateComplexSingleLineText(101));
        yield return NewValidRequest(x => x.Description = string.Empty);
        yield return NewValidRequest(x => x.Description = RandomStringUtil.GenerateComplexSingleLineText(501));
        yield return NewValidRequest(x => x.Position = 0);
        yield return NewValidRequest(x => x.Position = 101);
    }

    private CreateMajorityElectionBallotGroupRequest NewValidRequest(Action<CreateMajorityElectionBallotGroupRequest>? action = null)
    {
        var request = new CreateMajorityElectionBallotGroupRequest
        {
            MajorityElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ShortDescription = "short",
            Description = "test",
            Position = 27,
            Entries = { MajorityElectionBallotGroupEntryTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
