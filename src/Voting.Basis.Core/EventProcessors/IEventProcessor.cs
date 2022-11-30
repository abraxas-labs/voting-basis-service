// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Google.Protobuf;
using Voting.Lib.Eventing.Subscribe;

namespace Voting.Basis.Core.EventProcessors;

public interface IEventProcessor<TEvent> : IEventProcessor<EventProcessorScope, TEvent>
    where TEvent : IMessage<TEvent>
{
}
