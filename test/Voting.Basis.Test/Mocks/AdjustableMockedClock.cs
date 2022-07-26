// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Common;
using Voting.Lib.Testing.Mocks;

namespace Voting.Basis.Test.Mocks;

public class AdjustableMockedClock : IClock
{
    public static DateTime? OverrideUtcNow { get; set; }

    public DateTime UtcNow => OverrideUtcNow ?? MockedClock.UtcNowDate;
}
