// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Messages;

public class CountingCircleChangeMessage
{
    public CountingCircleChangeMessage(BaseEntityMessage<CountingCircle> countingCircle)
    {
        CountingCircle = countingCircle;
    }

    public BaseEntityMessage<CountingCircle> CountingCircle { get; }
}
