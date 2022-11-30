// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Domain;

public class EntityOrder
{
    public Guid Id { get; set; }

    public int Position { get; set; }
}
