// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Basis.Data.Models;

public class PoliticalAssembly : BaseEntity
{
    public DateTime Date { get; set; }

    public Dictionary<string, string> Description { get; set; } = [];

    public Guid DomainOfInfluenceId { get; set; }

    public DomainOfInfluence DomainOfInfluence { get; set; } = null!; // set by ef

    public PoliticalAssemblyState State { get; set; } = PoliticalAssemblyState.Active;

    public DateTime? ArchivePer { get; set; }

    public DateTime PastLockPer { get; set; }
}
