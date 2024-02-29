// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Basis.Test;

public class TestApplicationFactory : BaseTestApplicationFactory<TestStartup>, IAsyncLifetime
{
    Task IAsyncLifetime.InitializeAsync()
    {
        return Services.GetRequiredService<InMemoryTestHarness>().Start();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return Services.GetRequiredService<InMemoryTestHarness>().Stop();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder
            .UseEnvironment("Test")
            .UseSolutionRelativeContentRoot("src/Voting.Basis");
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        return base.CreateHostBuilder()
            .UseSerilog((context, _, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
            .ConfigureAppConfiguration((_, config) =>
            {
                // we deploy our config with the docker image, no need to watch for changes
                foreach (var source in config.Sources.OfType<JsonConfigurationSource>())
                {
                    source.ReloadOnChange = false;
                }
            });
    }
}
