// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Voting.Basis.Test.ProtoValidatorTests.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.MajorityElection;

public class UpdateMajorityElectionBallotGroupCandidatesRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionBallotGroupCandidatesRequest>
{
    protected override IEnumerable<UpdateMajorityElectionBallotGroupCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.EntryCandidates.Clear());
    }

    protected override IEnumerable<UpdateMajorityElectionBallotGroupCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BallotGroupId = "invalid-guid");
        yield return NewValidRequest(x => x.BallotGroupId = string.Empty);
    }

    private UpdateMajorityElectionBallotGroupCandidatesRequest NewValidRequest(Action<UpdateMajorityElectionBallotGroupCandidatesRequest>? action = null)
    {
        var request = new UpdateMajorityElectionBallotGroupCandidatesRequest
        {
            BallotGroupId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            EntryCandidates = { MajorityElectionBallotGroupEntryCandidatesTest.NewValid() },
        };

        action?.Invoke(request);
        return request;
    }
}
