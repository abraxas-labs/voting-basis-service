// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class MajorityElectionBallotGroupEntryCandidatesTest : ProtoValidatorBaseTest<MajorityElectionBallotGroupEntryCandidates>
{
    public static MajorityElectionBallotGroupEntryCandidates NewValid(Action<MajorityElectionBallotGroupEntryCandidates>? action = null)
    {
        var majorityElectionBallotGroupEntryCandidates = new MajorityElectionBallotGroupEntryCandidates
        {
            BallotGroupEntryId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            CandidateIds = { "da36912c-7eaf-43fe-86d4-70c816f17c5a" },
            IndividualCandidatesVoteCount = 27,
        };

        action?.Invoke(majorityElectionBallotGroupEntryCandidates);
        return majorityElectionBallotGroupEntryCandidates;
    }

    protected override IEnumerable<MajorityElectionBallotGroupEntryCandidates> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.CandidateIds.Clear());
        yield return NewValid(x => x.IndividualCandidatesVoteCount = 0);
        yield return NewValid(x => x.IndividualCandidatesVoteCount = 100);
    }

    protected override IEnumerable<MajorityElectionBallotGroupEntryCandidates> NotOkMessages()
    {
        yield return NewValid(x => x.BallotGroupEntryId = "invalid-guid");
        yield return NewValid(x => x.BallotGroupEntryId = string.Empty);
        yield return NewValid(x => x.CandidateIds.Add("invalid-guid"));
        yield return NewValid(x => x.CandidateIds.Add(string.Empty));
        yield return NewValid(x => x.IndividualCandidatesVoteCount = -1);
        yield return NewValid(x => x.IndividualCandidatesVoteCount = 101);
    }
}
