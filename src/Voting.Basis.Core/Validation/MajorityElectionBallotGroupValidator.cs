// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Basis.Core.Domain;

namespace Voting.Basis.Core.Validation;

public class MajorityElectionBallotGroupValidator : AbstractValidator<MajorityElectionBallotGroup>
{
    public MajorityElectionBallotGroupValidator()
    {
        RuleFor(bg => bg.Entries).Must(e => e.Count > 0);
    }
}
