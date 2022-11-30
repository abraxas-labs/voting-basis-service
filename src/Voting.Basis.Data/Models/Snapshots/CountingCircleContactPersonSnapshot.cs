// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models.Snapshots;

public class CountingCircleContactPersonSnapshot : BaseCountingCircleContactPerson
{
    public CountingCircleSnapshot? CountingCircleDuringEvent { get; set; }

    public Guid? CountingCircleDuringEventId { get; set; }

    public CountingCircleSnapshot? CountingCircleAfterEvent { get; set; }

    public Guid? CountingCircleAfterEventId { get; set; }
}
