// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class MajorityElectionBallotGroupEntryTest : ProtoValidatorBaseTest<MajorityElectionBallotGroupEntry>
{
    public static MajorityElectionBallotGroupEntry NewValid(Action<MajorityElectionBallotGroupEntry>? action = null)
    {
        var majorityElectionBallotGroupEntry = new MajorityElectionBallotGroupEntry
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            ElectionId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            BlankRowCount = 27,
            IndividualCandidatesVoteCount = 27,
            CountOfCandidates = 27,
            CandidateCountOk = true,
        };

        action?.Invoke(majorityElectionBallotGroupEntry);
        return majorityElectionBallotGroupEntry;
    }

    protected override IEnumerable<MajorityElectionBallotGroupEntry> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.BlankRowCount = 0);
        yield return NewValid(x => x.BlankRowCount = 100);
        yield return NewValid(x => x.IndividualCandidatesVoteCount = 0);
        yield return NewValid(x => x.IndividualCandidatesVoteCount = 100);
        yield return NewValid(x => x.CountOfCandidates = 0);
        yield return NewValid(x => x.CountOfCandidates = 100);
        yield return NewValid(x => x.CandidateCountOk = false);
    }

    protected override IEnumerable<MajorityElectionBallotGroupEntry> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.ElectionId = "invalid-guid");
        yield return NewValid(x => x.ElectionId = string.Empty);
        yield return NewValid(x => x.BlankRowCount = -1);
        yield return NewValid(x => x.BlankRowCount = 101);
        yield return NewValid(x => x.IndividualCandidatesVoteCount = -1);
        yield return NewValid(x => x.IndividualCandidatesVoteCount = 101);
        yield return NewValid(x => x.CountOfCandidates = -1);
        yield return NewValid(x => x.CountOfCandidates = 101);
    }
}
