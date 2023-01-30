// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Voting.Basis.Ech.Schemas;

internal static class EchSchemaLoader
{
    internal static XmlSchemaSet LoadEchSchemas(Dictionary<string, string> schemas)
    {
        var schemaDirectory = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Schemas");
        var xmlSchemaSet = new XmlSchemaSet();
        foreach (var (schemaName, schemaFileName) in schemas)
        {
            var schemaPath = Path.Join(schemaDirectory, schemaFileName);
            using var xmlReader = XmlReader.Create(schemaPath);
            xmlSchemaSet.Add(schemaName, xmlReader);
        }

        return xmlSchemaSet;
    }
}
