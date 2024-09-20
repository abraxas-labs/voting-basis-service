// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Test.MockedData.Mapping;

public class DomainOfInfluenceTestPartyProfile : Profile
{
    public DomainOfInfluenceTestPartyProfile()
    {
        CreateMap<DomainOfInfluenceParty, Core.Domain.DomainOfInfluenceParty>();
    }
}
