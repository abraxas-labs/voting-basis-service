// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Domain;

public class CountingCircleResultStateDescription
{
    public CountingCircleResultState State { get; set; }

    public string Description { get; set; } = string.Empty;
}
