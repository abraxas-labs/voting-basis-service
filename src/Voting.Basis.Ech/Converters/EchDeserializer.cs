// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Xml.Schema;
using System.Xml.Serialization;
using Voting.Lib.Common;

namespace Voting.Basis.Ech.Converters;

internal static class EchDeserializer
{
    internal static T DeserializeXml<T>(string xml, XmlSchemaSet schemaSet)
    {
        using var sr = new StringReader(xml);
        using var reader = XmlUtil.CreateReaderWithSchemaValidation(sr, schemaSet);

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T?)serializer.Deserialize(reader)
                ?? throw new ValidationException("Deserialization returned null");
        }
        catch (InvalidOperationException ex) when (ex.InnerException != null)
        {
            // The XmlSerializer wraps all exceptions into an InvalidOperationException.
            // Unwrap it to surface the "correct" exception type.
            throw ex.InnerException;
        }
    }
}
