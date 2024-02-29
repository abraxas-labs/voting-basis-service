// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class CountingCircleElectorate : BaseEntity
{
    public List<DomainOfInfluenceType> DomainOfInfluenceTypes { get; set; } = new();

    public Guid CountingCircleId { get; set; }

    public CountingCircle CountingCircle { get; set; } = null!;
}
