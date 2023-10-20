// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using eCH_0155_4_0;
using Voting.Lib.Ech.Ech0157.Models;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class ElectionMapping
{
    internal static ElectionDescriptionInformationType ToEchElectionDescription(this DataModels.PoliticalBusiness election)
    {
        var descriptionInfos = new List<ElectionDescriptionInfoType>();

        foreach (var (lang, officialDescription) in election.OfficialDescription)
        {
            election.ShortDescription.TryGetValue(lang, out var shortDescription);

            // Truncating to 255, since eCH doesn't allow any longer strings in this field.
            descriptionInfos.Add(ElectionDescriptionInfoType.Create(lang, officialDescription.Truncate(255), shortDescription));
        }

        return ElectionDescriptionInformationType.Create(descriptionInfos);
    }

    internal static ElectionInformationExtension? GetExtension(object? extension)
    {
        if (extension == null)
        {
            return null;
        }

        var extensionChildNodes = extension as XmlNode[]
            ?? throw new InvalidOperationException("Election information extension not set as XML node");

        var childElement = extensionChildNodes.FirstOrDefault(n => n is XmlElement);
        if (childElement == null)
        {
            return null;
        }

        var extensionNode = childElement?.ParentNode
            ?? throw new InvalidOperationException("Election information extension child node has no parent node");

        using var reader = new StringReader(extensionNode.OuterXml);
        var electionInformationExtension = DeserializeXmlNode<ElectionInformationExtension>(reader);

        return electionInformationExtension;
    }

    private static T? DeserializeXmlNode<T>(TextReader reader)
    {
        var serializer = new XmlSerializer(typeof(T));
        return (T?)serializer.Deserialize(reader);
    }
}
