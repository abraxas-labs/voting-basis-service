// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Configuration;

public class MachineConfig
{
    public string Name { get; set; } = Environment.MachineName;
}
