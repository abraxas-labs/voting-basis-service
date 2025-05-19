// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voting.Basis.Core.Messaging;
using Voting.Basis.Test.MockedData.Mapping;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Common;
using Voting.Lib.Ech;
using Voting.Lib.MalwareScanner.Services;
using Voting.Lib.Messaging;
using Voting.Lib.ObjectStorage;
using Voting.Lib.Testing.Mocks;

namespace Voting.Basis.Test;

public class TestStartup : Startup
{
    public TestStartup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration, environment)
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
            .AddSingleton<TestMapper>()
            .AddMock<IMalwareScannerService, MockMalwareScannerService>();
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
        services.AddVotingLibMessagingMocks(o => o.AddConsumerAndConsumerTestHarness<MessageConsumer<EventProcessedMessage>>());
    }
}
