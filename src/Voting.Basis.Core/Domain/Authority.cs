// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Core.Domain;

public class Authority
{
    public Authority()
    {
        SecureConnectId = string.Empty;
    }

    /// <summary>
    /// Gets the tenant ID of this authority.
    /// </summary>
    public string SecureConnectId { get; internal set; }

    public string? Name { get; internal set; }

    public string? Phone { get; internal set; }

    public string? Email { get; internal set; }

    public string? Street { get; internal set; }

    public string? Zip { get; internal set; }

    public string? City { get; internal set; }
}
