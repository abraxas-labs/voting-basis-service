// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Voting.Basis.Ech.Models;

namespace Voting.Basis.Ech.Utils;

public static class CountryUtils
{
    public const int SwissCountryId = 8100;
    public const string SwissCountryIso = "CH";
    public const string SwissCountryNameShort = "Schweiz";

    private const string BfsCountryListFile = "Voting.Basis.Ech.Files.Utils.BFSCountryList.xml";
    private static readonly List<Country> Countries = GetCountryList();

    public static List<Country> GetAll()
    {
        return Countries;
    }

    public static Country? GetCountryFromIsoId(string isoId)
    {
        return Countries.Find(x => x.IsoId.Equals(isoId, StringComparison.InvariantCultureIgnoreCase));
    }

    private static List<Country> GetCountryList()
    {
        var serializer = new XmlSerializer(typeof(CountryXmlRootModel));
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(BfsCountryListFile)
                           ?? throw new FileNotFoundException(BfsCountryListFile);

        using var reader = new StreamReader(stream);
        var rootModel = serializer.Deserialize(reader) as CountryXmlRootModel;

        ArgumentNullException.ThrowIfNull(rootModel?.Country);

        return rootModel.Country
            .Where(x => x.EntryValid && (x.RecognizedCh || x.IsoId.Equals(SwissCountryIso, StringComparison.InvariantCultureIgnoreCase)))
            .OrderBy(x => x.Description)
            .ToList();
    }
}
