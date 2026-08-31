// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using Voting.Lib.Iam.Models;

namespace Voting.Basis.Core.Extensions;

public static class UserExtensions
{
    public static EventInfoUser ToEventInfoUser(this User user)
    {
        return new()
        {
            Id = user.Loginid,
            FirstName = user.Firstname ?? string.Empty,
            LastName = user.Lastname ?? string.Empty,
            Username = user.Username ?? string.Empty,
        };
    }

    public static Data.Models.User ToDataUser(this User user)
    {
        return user.ToEventInfoUser().ToDataUser();
    }

    public static Data.Models.User ToDataUser(this EventInfoUser user)
    {
        return new()
        {
            SecureConnectId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
    }
}
