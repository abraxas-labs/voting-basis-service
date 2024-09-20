// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Messages;

public class ProportionalElectionListChangeMessage
{
    public ProportionalElectionListChangeMessage(BaseEntityMessage<ProportionalElectionList> list)
    {
        List = list;
    }

    public BaseEntityMessage<ProportionalElectionList> List { get; }
}
