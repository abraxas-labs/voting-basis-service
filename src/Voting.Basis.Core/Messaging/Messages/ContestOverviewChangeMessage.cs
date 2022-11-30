// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Messages;

public class ContestOverviewChangeMessage
{
    public ContestOverviewChangeMessage(BaseEntityMessage<Contest> contest)
    {
        Contest = contest;
    }

    public BaseEntityMessage<Contest> Contest { get; }
}
