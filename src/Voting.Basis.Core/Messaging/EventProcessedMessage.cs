// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Basis.Core.Messaging;

public record EventProcessedMessage(
    string EventType,
    string TenantId,
    Guid? AggregateId,
    Guid? EntityId,
    Guid? ContestId,
    Guid? PoliticalBusinessId,
    Guid? DomainOfInfluenceId,
    Guid? CountingCircleId);
