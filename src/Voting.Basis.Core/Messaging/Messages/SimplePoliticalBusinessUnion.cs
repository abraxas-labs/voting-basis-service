// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Messages;

// masstransit works only with interfaces or non-abstract classes.
public class SimplePoliticalBusinessUnion : PoliticalBusinessUnion
{
    public override PoliticalBusinessUnionType Type => UnionType;

    public PoliticalBusinessUnionType UnionType { get; set; }
}
