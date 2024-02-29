// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using Voting.Lib.Iam.Models;

namespace Voting.Basis.Core.Extensions;

public static class TenantExtensions
{
    public static EventInfoTenant ToEventInfoTenant(this Tenant tenant)
    {
        return new()
        {
            Id = tenant.Id,
            Name = tenant.Name ?? string.Empty,
        };
    }
}
