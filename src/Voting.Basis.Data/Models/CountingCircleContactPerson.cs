// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Data.Models;

// due to ef core 3 limitations it is not possible to extend the ContactPerson entity (owned vs non owned)
public class CountingCircleContactPerson : BaseCountingCircleContactPerson
{
    public CountingCircle? CountingCircleDuringEvent { get; set; }

    public Guid? CountingCircleDuringEventId { get; set; }

    public CountingCircle? CountingCircleAfterEvent { get; set; }

    public Guid? CountingCircleAfterEventId { get; set; }
}
