// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Basis.Data.Models;
using Voting.Basis.Data.Models.Snapshots;

namespace Voting.Basis.Core.Utils;

public static class DomainOfInfluenceTreeBuilder
{
    /// <summary>
    /// Builds a hierarchical tree of domain of influences.
    /// </summary>
    /// <param name="flatDomains">A flat, unsorted list of domain of influences.</param>
    /// <param name="countingCirclesByDomainOfInfluence">The counting circles grouped by domain of influence IDs.</param>
    /// <returns>A list of root domain of influences, where each domain of influence has a list of children.</returns>
    internal static List<DomainOfInfluence> BuildTree(
        List<DomainOfInfluence> flatDomains,
        Dictionary<Guid, List<DomainOfInfluenceCountingCircle>>? countingCirclesByDomainOfInfluence = null)
    {
        if (flatDomains.Count == 0)
        {
            return flatDomains;
        }

        var byParentId = flatDomains
            .GroupBy(x => x.ParentId ?? Guid.Empty) // empty guid for root domain of influences
            .ToDictionary(x => x.Key, x => x.ToList());
        var byId = flatDomains
            .ToDictionary(x => x.Id);

        foreach (var domainOfInfluence in flatDomains)
        {
            domainOfInfluence.Children = new List<DomainOfInfluence>();
        }

        foreach (var (parentId, dois) in byParentId)
        {
            if (parentId == default)
            {
                continue;
            }

            var parent = byId[parentId];
            foreach (var doi in dois.OrderBy(doi => doi.Name))
            {
                doi.Parent = parent;
                parent.Children.Add(doi);
            }
        }

        var tree = byParentId[Guid.Empty].OrderBy(d => d.Name).ToList();
        if (countingCirclesByDomainOfInfluence == null)
        {
            return tree;
        }

        foreach (var (id, circles) in countingCirclesByDomainOfInfluence)
        {
            byId[id].CountingCircles = circles.OrderBy(x => x.CountingCircle.Name).ToList();
        }

        return tree;
    }

    internal static List<DomainOfInfluenceSnapshot> BuildTree(
        List<DomainOfInfluenceSnapshot> flatDomains,
        Dictionary<Guid, List<DomainOfInfluenceCountingCircleSnapshot>>? countingCirclesByDomainOfInfluence = null)
    {
        if (flatDomains.Count == 0)
        {
            return flatDomains;
        }

        var byParentId = flatDomains
            .GroupBy(x => x.BasisParentId ?? Guid.Empty)
            .ToDictionary(x => x.Key, x => x.ToList()); // empty guid for roots
        var byId = flatDomains
            .ToDictionary(x => x.BasisId);

        foreach (var (parentId, dois) in byParentId)
        {
            if (parentId == default)
            {
                continue;
            }

            var parent = byId[parentId];
            foreach (var doi in dois.OrderBy(doi => doi.Name))
            {
                doi.Parent = parent;
                parent.Children.Add(doi);
            }
        }

        var tree = byParentId[Guid.Empty].OrderBy(d => d.Name).ToList();
        if (countingCirclesByDomainOfInfluence == null)
        {
            return tree;
        }

        foreach (var (id, circles) in countingCirclesByDomainOfInfluence)
        {
            if (byId.ContainsKey(id))
            {
                byId[id].CountingCircles = circles.OrderBy(x => x.CountingCircle.Name).ToList();
            }
        }

        return tree;
    }
}
