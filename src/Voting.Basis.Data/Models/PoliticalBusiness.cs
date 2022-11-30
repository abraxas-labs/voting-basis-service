// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public abstract class PoliticalBusiness : BaseEntity
{
    public string PoliticalBusinessNumber { get; set; } = string.Empty;

    public Dictionary<string, string> OfficialDescription { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();

    public string InternalDescription { get; set; } = string.Empty;

    public virtual bool Active { get; set; }

    public virtual Guid DomainOfInfluenceId { get; set; }

    public virtual DomainOfInfluence? DomainOfInfluence { get; set; }

    public virtual Guid ContestId { get; set; }

    public virtual Contest Contest { get; set; } = null!;

    public abstract PoliticalBusinessType PoliticalBusinessType { get; }
}
