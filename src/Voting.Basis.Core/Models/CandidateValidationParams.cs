// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Models;

public class CandidateValidationParams(DomainOfInfluence doi, bool testingPhaseEnded, bool? onlyNamesAndNumberRequired = null)
{
    public DomainOfInfluenceType DoiType { get; } = doi.Type;

    public bool IsLocalityRequired { get; } = doi.CantonDefaults.CandidateLocalityRequired && !onlyNamesAndNumberRequired.GetValueOrDefault();

    public bool IsOriginRequired { get; } = doi.CantonDefaults.CandidateOriginRequired && !onlyNamesAndNumberRequired.GetValueOrDefault();

    public bool TestingPhaseEnded { get; } = testingPhaseEnded;

    public bool? OnlyNamesAndNumberRequired { get; } = onlyNamesAndNumberRequired;
}
