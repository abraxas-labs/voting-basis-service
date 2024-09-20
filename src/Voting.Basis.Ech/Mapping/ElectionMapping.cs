// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Ech0155_4_0;
using Voting.Lib.Ech.Ech0157_4_0.Models;
using DataModels = Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Mapping;

internal static class ElectionMapping
{
    internal static ElectionDescriptionInformationType ToEchElectionDescription(this DataModels.PoliticalBusiness election)
    {
        var descriptionInfos = new ElectionDescriptionInformationType();

        foreach (var (lang, officialDescription) in election.OfficialDescription)
        {
            election.ShortDescription.TryGetValue(lang, out var shortDescription);

            // Truncating to 255, since eCH doesn't allow any longer strings in this field.
            descriptionInfos.ElectionDescriptionInfo.Add(new ElectionDescriptionInformationTypeElectionDescriptionInfo
            {
                Language = lang,
                ElectionDescription = officialDescription.Truncate(255),
                ElectionDescriptionShort = shortDescription,
            });
        }

        return descriptionInfos;
    }

    internal static ElectionInformationExtension? GetExtension(IEnumerable<XmlElement>? extension)
    {
        var extensionNode = extension?.FirstOrDefault();
        if (extensionNode == null)
        {
            return null;
        }

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
