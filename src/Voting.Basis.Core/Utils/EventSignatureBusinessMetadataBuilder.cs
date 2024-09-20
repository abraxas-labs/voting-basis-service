// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1.Metadata;

namespace Voting.Basis.Core.Utils;

internal static class EventSignatureBusinessMetadataBuilder
{
    public static EventSignatureBusinessMetadata BuildFrom(Guid contestId) => new() { ContestId = contestId.ToString() };
}
