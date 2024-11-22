// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Messages;

// masstransit works only with interfaces or non-abstract classes.
public class SimplePoliticalBusinessUnion : PoliticalBusinessUnion
{
    public override PoliticalBusinessUnionType Type => UnionType;

    public PoliticalBusinessUnionType UnionType { get; set; }

    public List<Guid> PoliticalBusinessIds { get; set; } = new();
}
