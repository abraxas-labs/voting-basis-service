// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Converters;

public interface IEch0159Deserializer
{
    Contest DeserializeXml(Stream stream);
}
