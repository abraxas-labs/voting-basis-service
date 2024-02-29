// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class ComparisonCountOfVotersConfiguration
{
    public ComparisonCountOfVotersCategory Category { get; private set; }

    public decimal? ThresholdPercent { get; private set; }
}
