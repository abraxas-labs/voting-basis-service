// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Models;
public class CandidateValidationParams(DomainOfInfluence doi)
{
    public DomainOfInfluenceType DoiType { get; set; } = doi.Type;

    public bool IsLocalityRequired { get; set; } = doi.CantonDefaults.CandidateLocalityRequired;

    public bool IsOriginRequired { get; set; } = doi.CantonDefaults.CandidateOriginRequired;
}
