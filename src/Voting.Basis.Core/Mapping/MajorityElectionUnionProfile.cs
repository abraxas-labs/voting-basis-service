﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Mapping;

public class MajorityElectionUnionProfile : Profile
{
    public MajorityElectionUnionProfile()
    {
        CreateMap<MajorityElectionUnionEventData, MajorityElectionUnion>();
    }
}
