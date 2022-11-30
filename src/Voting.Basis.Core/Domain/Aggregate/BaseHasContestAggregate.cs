// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using Google.Protobuf;
using Voting.Basis.Core.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Basis.Core.Domain.Aggregate;

public abstract class BaseHasContestAggregate : BaseDeletableAggregate
{
    public Guid ContestId { get; protected set; }

    public abstract void MoveToNewContest(Guid newContestId);

    public void RaiseEvent(IMessage eventData)
    {
        RaiseEvent(eventData, EventSignatureBusinessMetadataBuilder.BuildFrom(ContestId));
    }

    protected void EnsureDifferentContest(Guid newContestId)
    {
        if (ContestId == newContestId)
        {
            throw new ValidationException($"The contest id {newContestId} must be different from the existing contest id");
        }
    }
}
