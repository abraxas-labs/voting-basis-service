// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Basis.Core.Configuration;

namespace Voting.Basis.Test.ServiceModes;

public class PublisherServiceModeTest : BaseServiceModeTest<PublisherServiceModeTest.PublisherAppFactory>
{
    public PublisherServiceModeTest(PublisherAppFactory factory)
        : base(factory, ServiceMode.Publisher)
    {
    }

    public class PublisherAppFactory : ServiceModeAppFactory
    {
        public PublisherAppFactory()
            : base(ServiceMode.Publisher)
        {
        }
    }
}
