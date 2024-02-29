// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Configuration;

public class PortConfig
{
    public ushort Http { get; set; } = 5000;

    public ushort Http2 { get; set; } = 5001;
}
