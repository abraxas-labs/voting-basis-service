// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Messaging.Messages;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Core.Messaging.Extensions;

public static class CountingCircleExtensions
{
    public static BaseEntityMessage<CountingCircle> CreateBaseEntityEvent(this CountingCircle countingCircle, EntityState entityState)
    {
        return new BaseEntityMessage<CountingCircle>(
            new()
            {
                Id = countingCircle.Id,
                Canton = countingCircle.Canton,
                ResponsibleAuthority = new Authority
                {
                    SecureConnectId = countingCircle.ResponsibleAuthority.SecureConnectId,
                },
            },
            entityState);
    }
}
