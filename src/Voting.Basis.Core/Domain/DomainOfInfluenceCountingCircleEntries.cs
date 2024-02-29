// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Basis.Core.Domain;

public class DomainOfInfluenceCountingCircleEntries
{
    public Guid Id { get; set; }

    public IReadOnlyCollection<Guid> CountingCircleIds { get; set; } = Array.Empty<Guid>();
}
