// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Models;

public class PoliticalBusinessSummary
{
    public PoliticalBusiness PoliticalBusiness { get; set; } = null!; // loaded by ef

    public string PoliticalBusinessUnionDescription { get; set; } = string.Empty;

    public Guid? PoliticalBusinessUnionId { get; set; }

    public string ElectionGroupNumber { get; set; } = string.Empty;

    public Guid? ElectionGroupId { get; set; }
}
