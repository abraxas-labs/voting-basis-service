// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using FluentAssertions;
using Voting.Basis.Data.Models;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Lib.VotingExports.Models;
using Xunit;
using DomainOfInfluenceType = Voting.Basis.Data.Models.DomainOfInfluenceType;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.EnumTests;

public class EnumTest : BaseTest
{
    private readonly TestMapper _mapper;

    public EnumTest(TestApplicationFactory factory)
        : base(factory)
    {
        _mapper = GetService<TestMapper>();
    }

    [Theory]
    [InlineData(typeof(SexType), typeof(SharedProto.SexType))]
    [InlineData(typeof(EntityType), typeof(SharedProto.ExportEntityType))]
    [InlineData(typeof(BallotNumberGeneration), typeof(SharedProto.BallotNumberGeneration))]
    [InlineData(typeof(CantonMajorityElectionAbsoluteMajorityAlgorithm), typeof(SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm))]
    [InlineData(typeof(SwissAbroadVotingRight), typeof(SharedProto.SwissAbroadVotingRight))]
    [InlineData(typeof(ContestState), typeof(SharedProto.ContestState))]
    [InlineData(typeof(CountingCircleState), typeof(SharedProto.CountingCircleState))]
    [InlineData(typeof(DomainOfInfluenceType), typeof(SharedProto.DomainOfInfluenceType))]
    [InlineData(typeof(DomainOfInfluenceCanton), typeof(SharedProto.DomainOfInfluenceCanton))]
    [InlineData(typeof(MajorityElectionMandateAlgorithm), typeof(SharedProto.MajorityElectionMandateAlgorithm))]
    [InlineData(typeof(MajorityElectionResultEntry), typeof(SharedProto.MajorityElectionResultEntry))]
    [InlineData(typeof(PoliticalBusinessType), typeof(SharedProto.PoliticalBusinessType))]
    [InlineData(typeof(ProportionalElectionMandateAlgorithm), typeof(SharedProto.ProportionalElectionMandateAlgorithm))]
    [InlineData(typeof(BallotType), typeof(SharedProto.BallotType))]
    [InlineData(typeof(VoteResultAlgorithm), typeof(SharedProto.VoteResultAlgorithm))]
    [InlineData(typeof(VoteResultEntry), typeof(SharedProto.VoteResultEntry))]
    [InlineData(typeof(EntityState), typeof(ProtoModels.EntityState))]
    [InlineData(typeof(VotingCardShippingFranking), typeof(SharedProto.VotingCardShippingFranking))]
    [InlineData(typeof(VotingCardShippingMethod), typeof(SharedProto.VotingCardShippingMethod))]
    [InlineData(typeof(ComparisonCountOfVotersCategory), typeof(SharedProto.ComparisonCountOfVotersCategory))]
    [InlineData(typeof(VotingChannel), typeof(SharedProto.VotingChannel))]
    [InlineData(typeof(VotingApp), typeof(SharedProto.ExportGenerator))]
    public void ShouldBeSameEnum(Type dataEnumType, Type protoEnumType)
    {
        CompareEnums(dataEnumType, protoEnumType);
        MappingTest(dataEnumType, protoEnumType);
    }

    [Theory]
    [InlineData(typeof(PoliticalBusinessUnionType), typeof(SharedProto.PoliticalBusinessUnionType), "PoliticalBusinessUnion")]
    public void ShouldBeSameEnumWithPrefix(Type dataEnumType, Type protoEnumType, string prefix)
    {
        CompareEnums(dataEnumType, protoEnumType, prefix);
        MappingTest(dataEnumType, protoEnumType);
    }

    [Fact]
    public void ShouldThrowAutoMapperException()
    {
        Assert.Throws<AutoMapperMappingException>(() => _mapper.Map<EnumMockedData.TestEnum1>(EnumMockedData.TestEnum2.ValueC));
        Assert.Throws<AutoMapperMappingException>(() => _mapper.Map<EnumMockedData.TestEnum1>(EnumMockedData.TestEnum2.ValueB2));
    }

    private static void CompareEnums(Type dataEnumType, Type protoEnumType, string? prefix = null)
    {
        var dataEnumArray = (int[])Enum.GetValues(dataEnumType);
        var protoEnumArray = (int[])Enum.GetValues(protoEnumType);

        dataEnumArray.Length.Should().Be(protoEnumArray.Length);

        foreach (var value in dataEnumArray)
        {
            var dataEnumName = Enum.GetName(dataEnumType, value);
            if (prefix != null)
            {
                dataEnumName = prefix + dataEnumName;
            }

            var protoEnumName = Enum.GetName(protoEnumType, value);
            dataEnumName.Should().Be(protoEnumName);
        }

        for (var i = 0; i < protoEnumArray.Length; i++)
        {
            dataEnumArray[i].Should().Be(protoEnumArray[i]);
        }
    }

    private void MappingTest(Type dataEnumType, Type protoEnumType)
    {
        var dataEnumArray = (int[])Enum.GetValues(dataEnumType);
        var protoEnumArray = (int[])Enum.GetValues(protoEnumType);

        for (var i = 0; i < protoEnumArray.Length; i++)
        {
            var dataEnum = Enum.ToObject(dataEnumType, dataEnumArray[i]);
            var protoEnum = Enum.ToObject(protoEnumType, protoEnumArray[i]);

            var mappedProtoValue = _mapper.Map(dataEnum, dataEnumType, protoEnumType);
            mappedProtoValue.Should().Be(protoEnum);

            var mappedDataValue = _mapper.Map(protoEnum, protoEnumType, dataEnumType);
            mappedDataValue.Should().Be(dataEnum);
        }
    }
}
