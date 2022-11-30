// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class EventProcessingState : BaseEntity
{
    public static readonly Guid StaticId = new("64a18c02-b079-4ff0-891a-bf84f45e292a");

    public EventProcessingState()
    {
        Id = StaticId;
    }

    public ulong CommitPosition { get; set; }

    public ulong PreparePosition { get; set; }

    public ulong EventNumber { get; set; }
}
