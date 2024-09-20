// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Messages;

public class ContestDetailsChangeMessage
{
    public ContestDetailsChangeMessage(
        BaseEntityMessage<SimplePoliticalBusiness>? politicalBusiness = null,
        BaseEntityMessage<SimplePoliticalBusinessUnion>? politicalBusinessUnion = null,
        BaseEntityMessage<ElectionGroup>? electionGroup = null)
    {
        PoliticalBusiness = politicalBusiness;
        PoliticalBusinessUnion = politicalBusinessUnion;
        ElectionGroup = electionGroup;
    }

    public BaseEntityMessage<SimplePoliticalBusiness>? PoliticalBusiness { get; }

    public BaseEntityMessage<SimplePoliticalBusinessUnion>? PoliticalBusinessUnion { get; }

    public BaseEntityMessage<ElectionGroup>? ElectionGroup { get; }

    public Guid? ContestId => PoliticalBusiness?.Data?.ContestId
        ?? PoliticalBusinessUnion?.Data?.ContestId
        ?? ElectionGroup?.Data?.PrimaryMajorityElection?.ContestId;
}
