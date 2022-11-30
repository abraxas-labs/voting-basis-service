// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public abstract class BaseCountingCircleContactPerson : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string MobilePhone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
