// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using Voting.Basis.Data.Models;
using Voting.Lib.VotingExports.Models;
using DomainOfInfluenceType = Voting.Basis.Data.Models.DomainOfInfluenceType;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Mapping;

/// <summary>
/// An entry for every enum is needed since AutoMapper doesnt have a property to enable mapByName globally.
/// If an entry for an enum is not defined AutoMapper will map the enums by their values.
/// </summary>
public class EnumProfile : Profile
{
    public EnumProfile()
    {
        CreateEnumMap<SharedProto.ExportEntityType, EntityType>();
        CreateEnumMap<SharedProto.BallotNumberGeneration, BallotNumberGeneration>();
        CreateEnumMap<SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm, CantonMajorityElectionAbsoluteMajorityAlgorithm>();
        CreateEnumMap<SharedProto.SwissAbroadVotingRight, SwissAbroadVotingRight>();
        CreateEnumMap<SharedProto.ContestState, ContestState>();
        CreateEnumMap<SharedProto.CountingCircleState, CountingCircleState>();
        CreateEnumMap<SharedProto.DomainOfInfluenceType, DomainOfInfluenceType>();
        CreateEnumMap<SharedProto.DomainOfInfluenceCanton, DomainOfInfluenceCanton>();
        CreateEnumMap<SharedProto.MajorityElectionMandateAlgorithm, MajorityElectionMandateAlgorithm>();
        CreateEnumMap<SharedProto.MajorityElectionResultEntry, MajorityElectionResultEntry>();
        CreateEnumMap<SharedProto.PoliticalBusinessType, PoliticalBusinessType>();
        CreateEnumMap<SharedProto.BallotType, BallotType>();
        CreateEnumMap<SharedProto.VoteResultAlgorithm, VoteResultAlgorithm>();
        CreateEnumMap<SharedProto.VoteResultEntry, VoteResultEntry>();
        CreateEnumMap<SharedProto.VotingCardShippingFranking, VotingCardShippingFranking>();
        CreateEnumMap<SharedProto.VotingCardShippingMethod, VotingCardShippingMethod>();
        CreateEnumMap<SharedProto.ComparisonCountOfVotersCategory, ComparisonCountOfVotersCategory>();
        CreateEnumMap<SharedProto.VotingChannel, VotingChannel>();
        CreateEnumMap<SharedProto.ExportGenerator, VotingApp>();
        CreateEnumMap<SharedProto.BallotQuestionType, BallotQuestionType>();

        // map by value since the political business union type enum in the proto has the wrong prefix.
        CreateMap<SharedProto.PoliticalBusinessUnionType, PoliticalBusinessUnionType>()
            .ConvertUsingEnumMapping(opt => opt.MapByValue())
            .ReverseMap();

        // explicitly map deprecated values to the corresponding new value.
        CreateMap<SharedProto.ProportionalElectionMandateAlgorithm, ProportionalElectionMandateAlgorithm>()
            .ConvertUsingEnumMapping(opt => opt
                .MapByName()
                .MapValue(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)
                .MapValue(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum))
            .ReverseMap();

        // explicitly map deprecated values to the corresponding new value.
        CreateMap<SharedProto.SexType, SexType>()
            .ConvertUsingEnumMapping(opt => opt
                .MapByName()
                .MapValue(SharedProto.SexType.Undefined, SexType.Female))
            .ReverseMap();

        // explicitly map deprecated values to default value.
        CreateMap<SharedProto.VotingCardColor, VotingCardColor>()
            .ConvertUsingEnumMapping(opt => opt
                .MapByValue()
                .MapValue(SharedProto.VotingCardColor.Chamois, VotingCardColor.Unspecified)
                .MapValue(SharedProto.VotingCardColor.Gold, VotingCardColor.Unspecified))
            .ReverseMap();
    }

    private void CreateEnumMap<T1, T2>()
        where T1 : struct, Enum
        where T2 : struct, Enum
    {
        CreateMap<T1, T2>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();
    }
}
