// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Basis.Data.Models;

/// <summary>
/// This is an entity which provides simpler access to political businesses of all types.
/// </summary>
public class SimplePoliticalBusiness : PoliticalBusiness
{
    public override PoliticalBusinessType PoliticalBusinessType => BusinessType;

    public PoliticalBusinessType BusinessType { get; set; }
}
