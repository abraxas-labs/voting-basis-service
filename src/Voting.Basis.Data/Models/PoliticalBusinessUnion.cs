﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public abstract class PoliticalBusinessUnion : BaseEntity
{
    public string Description { get; set; } = string.Empty;

    public string SecureConnectId { get; set; } = string.Empty;

    public Guid ContestId { get; set; }

    public Contest Contest { get; set; } = null!; // set by ef

    public abstract PoliticalBusinessUnionType Type { get; }
}
