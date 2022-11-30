// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Domain;

public class DomainOfInfluenceCountingCircle
{
    public Guid DomainOfInfluenceId { get; set; }

    public Guid CountingCircleId { get; set; }
}
