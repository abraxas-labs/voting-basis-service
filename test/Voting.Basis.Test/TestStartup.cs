// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Messaging.Consumers;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Common;
using Voting.Lib.Ech;
using Voting.Lib.ObjectStorage;
using Voting.Lib.Testing.Mocks;

namespace Voting.Basis.Test;

public class TestStartup : Startup
{
    public TestStartup(IConfiguration configuration)
        : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services
            .AddMock<IClock, AdjustableMockedClock>()
            .AddVotingLibEventingMocks()
            .AddVotingLibIamMocks()
            .AddVotingLibObjectStorageMock()
            .AddVotingLibCryptographyMocks()
            .AddVotingLibPkcs11Mock()
            .RemoveHostedServices()
            .AddHostedService<BucketInitializerService>()
            .AddMock<IEchMessageIdProvider, MockEchMessageIdProvider>()
            .AddScoped<TestEventPublisherAdapter>()
            .AddSingleton<TestMapper>();
    }

    protected override void ConfigureAuthentication(AuthenticationBuilder builder)
        => builder.AddMockedSecureConnectScheme();

    protected override void ConfigureDatabase(DbContextOptionsBuilder db)
    {
        base.ConfigureDatabase(db);
        db.AddNQueryDetector();
    }

    protected override void AddMessaging(IServiceCollection services)
    {
        services.AddVotingLibMessagingMocks(o =>
        {
            o.AddConsumerAndConsumerTestHarness<ContestDetailsChangeMessageConsumer>();
            o.AddConsumerAndConsumerTestHarness<ContestOverviewChangeMessageConsumer>();
        });
    }
}
