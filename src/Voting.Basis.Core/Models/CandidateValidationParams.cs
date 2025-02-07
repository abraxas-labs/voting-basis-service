// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Models;
public class CandidateValidationParams(DomainOfInfluence doi, bool onlyNamesAndNumberRequired = false)
{
    public DomainOfInfluenceType DoiType { get; } = doi.Type;

    public bool IsLocalityRequired { get; } = doi.CantonDefaults.CandidateLocalityRequired && !onlyNamesAndNumberRequired;

    public bool IsOriginRequired { get; } = doi.CantonDefaults.CandidateOriginRequired && !onlyNamesAndNumberRequired;

    public bool OnlyNamesAndNumberRequired { get; } = onlyNamesAndNumberRequired;
}
