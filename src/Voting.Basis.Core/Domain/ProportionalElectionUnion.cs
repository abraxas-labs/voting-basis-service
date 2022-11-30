// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;

namespace Voting.Basis.Core.Domain;

public class ProportionalElectionUnion : Profile
{
    public Guid Id { get; set; }

    public Guid ContestId { get; set; }

    public string SecureConnectId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
