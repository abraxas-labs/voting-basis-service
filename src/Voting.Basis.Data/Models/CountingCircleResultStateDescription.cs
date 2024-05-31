// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class CountingCircleResultStateDescription : BaseEntity
{
    public Guid CantonSettingsId { get; set; }

    public CantonSettings? CantonSettings { get; set; }

    public CountingCircleResultState State { get; set; }

    public string Description { get; set; } = string.Empty;
}
