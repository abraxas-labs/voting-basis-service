// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class ComparisonCountOfVotersCountingCircleEntry
{
    public Guid CountingCircleId { get; internal set; }

    public ComparisonCountOfVotersCategory Category { get; internal set; }
}
