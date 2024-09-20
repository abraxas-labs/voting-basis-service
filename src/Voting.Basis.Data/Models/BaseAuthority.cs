// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public abstract class BaseAuthority : BaseEntity
{
    public string SecureConnectId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Zip { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
}
