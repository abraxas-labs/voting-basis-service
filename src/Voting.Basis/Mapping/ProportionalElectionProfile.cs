// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Voting.Basis.Core.Messaging.Messages;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;

namespace Voting.Basis.Mapping;

public class ProportionalElectionProfile : Profile
{
    public ProportionalElectionProfile()
    {
        // write
        CreateMap<CreateProportionalElectionRequest, Core.Domain.ProportionalElection>();
        CreateMap<UpdateProportionalElectionRequest, Core.Domain.ProportionalElection>();
        CreateMap<CreateProportionalElectionListRequest, Core.Domain.ProportionalElectionList>();
        CreateMap<UpdateProportionalElectionListRequest, Core.Domain.ProportionalElectionList>();
        CreateMap<CreateProportionalElectionListUnionRequest, Core.Domain.ProportionalElectionListUnion>();
        CreateMap<UpdateProportionalElectionListUnionRequest, Core.Domain.ProportionalElectionListUnion>();
        CreateMap<UpdateProportionalElectionListUnionEntriesRequest, Core.Domain.ProportionalElectionListUnionEntries>()
            .ForMember(dst => dst.ProportionalElectionListIds, opts => opts.MapFrom(src => src.ProportionalElectionListIds));
        CreateMap<CreateProportionalElectionCandidateRequest, Core.Domain.ProportionalElectionCandidate>();
        CreateMap<UpdateProportionalElectionCandidateRequest, Core.Domain.ProportionalElectionCandidate>();

        // read
        CreateMap<Data.Models.ProportionalElection, ProtoModels.ProportionalElection>();
        CreateMap<IEnumerable<Data.Models.ProportionalElection>, ProtoModels.ProportionalElections>()
            .ForMember(dst => dst.ProportionalElections_, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.ProportionalElectionList, ProtoModels.ProportionalElectionList>()
            .ForMember(dst => dst.Party, opts => opts.MapFrom(src => src.Party != null
                ? src.Party
                : src.PartyId == null ? null : new Data.Models.DomainOfInfluenceParty { Id = src.PartyId.Value }));
        CreateMap<IEnumerable<Data.Models.ProportionalElectionList>, ProtoModels.ProportionalElectionLists>()
            .ForMember(dst => dst.Lists, opts => opts.MapFrom(src => src));

        CreateMap<Data.Models.ProportionalElectionListUnion, ProtoModels.ProportionalElectionListUnion>()
            .ForMember(dst => dst.ProportionalElectionSubListUnions, opts => opts.MapFrom(src => src.ProportionalElectionSubListUnions))
            .ForMember(dst => dst.ProportionalElectionListIds, opts => opts.MapFrom(src => src.ProportionalElectionListUnionEntries.Select(e => e.ProportionalElectionListId)));
        CreateMap<IEnumerable<Data.Models.ProportionalElectionListUnion>, ProtoModels.ProportionalElectionListUnions>()
            .ForMember(dst => dst.ProportionalElectionListUnions_, opts => opts.MapFrom(src => src));
        CreateMap<IEnumerable<Data.Models.ProportionalElectionListUnionEntry>, ProtoModels.ProportionalElectionListUnionEntries>()
            .ForMember(dst => dst.ProportionalElectionListIds, opts => opts.MapFrom(src => src));
        CreateMap<Data.Models.ProportionalElectionCandidate, ProtoModels.ProportionalElectionCandidate>()
            .ForMember(dst => dst.Party, opts => opts.MapFrom(src => src.Party != null
                ? src.Party
                : src.PartyId == null ? null : new Data.Models.DomainOfInfluenceParty { Id = src.PartyId.Value }));
        CreateMap<IEnumerable<Data.Models.ProportionalElectionCandidate>, ProtoModels.ProportionalElectionCandidates>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(src => src));

        CreateMap<BaseEntityMessage<Data.Models.ProportionalElectionList>, ProtoModels.ProportionalElectionListMessage>();
        CreateMap<ProportionalElectionListChangeMessage, ProtoModels.ProportionalElectionListChangeMessage>();
    }
}
