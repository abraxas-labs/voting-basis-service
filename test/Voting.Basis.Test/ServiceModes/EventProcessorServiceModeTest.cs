// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Configuration;

namespace Voting.Basis.Test.ServiceModes;

public class EventProcessorServiceModeTest : BaseServiceModeTest<EventProcessorServiceModeTest.EventProcessorAppFactory>
{
    public EventProcessorServiceModeTest(EventProcessorAppFactory factory)
        : base(factory, ServiceMode.EventProcessor)
    {
    }

    public class EventProcessorAppFactory : ServiceModeAppFactory
    {
        public EventProcessorAppFactory()
            : base(ServiceMode.EventProcessor)
        {
        }
    }
}
