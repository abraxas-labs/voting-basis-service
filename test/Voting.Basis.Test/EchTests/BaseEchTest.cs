// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Voting.Basis.Test.EchTests;

public abstract class BaseEchTest : BaseTest
{
    private static readonly Dictionary<string, string> Schemas = new()
    {
        ["http://www.ech.ch/xmlns/eCH-0159/4"] = "eCH-0159-4-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0157/4"] = "eCH-0157-4-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0155/4"] = "eCH-0155-4-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0058/5"] = "eCH-0058-5-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0044/4"] = "eCH-0044-4-1.xsd",
        ["http://www.ech.ch/xmlns/eCH-0010/6"] = "eCH-0010-6-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0008/3"] = "eCH-0008-3-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0007/6"] = "eCH-0007-6-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0006/2"] = "eCH-0006-2-0.xsd",
    };

    protected BaseEchTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected XmlSchemaSet BuildSchemaSet()
    {
        var schemaDirectory = Path.Join(
            TestSourcePaths.TestProjectSourceDirectory,
            "EchTests",
            "schemas");

        var xmlSchemaSet = new XmlSchemaSet();
        foreach (var (schemaName, schemaFileName) in Schemas)
        {
            var schemaPath = Path.Join(schemaDirectory, schemaFileName);
            xmlSchemaSet.Add(schemaName, XmlReader.Create(schemaPath));
        }

        return xmlSchemaSet;
    }

    protected void MatchXmlSnapshot(string xml, string fileName)
    {
        xml.MatchXmlSnapshot("EchTests", "_snapshots", fileName + ".xml");
    }
}
