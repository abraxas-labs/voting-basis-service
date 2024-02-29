// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
#if !DEBUG
using Serilog.Formatting.Json;
#endif
using Voting.Basis.Core.Configuration;
using Voting.Lib.Common;
using Voting.Lib.Database.Migrations;

namespace Voting.Basis;

public static class Program
{
    public static async Task Main(string[] args)
    {
        EnvironmentVariablesFixer.FixDotEnvironmentVariables();

        // A bootstrap logger which will be used until the app is completely initialized, since we cannot read from the config yet
        // Will be replaced later with the "real" logger
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
#if DEBUG
            .WriteTo.Console()
#else
            .WriteTo.Console(new JsonFormatter())
#endif
            .CreateBootstrapLogger();

        var host = CreateHostBuilder(args).Build();
        var config = host.Services.GetRequiredService<AppConfig>();

        // apps running in the publisher mode may only have read access to the database
        if (config.EventProcessorModeEnabled)
        {
            await host.Services.GetRequiredService<IDatabaseMigrator>().Migrate();
        }

        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, _, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
            .ConfigureAppConfiguration((_, config) =>
            {
                // we deploy our config with the docker image, no need to watch for changes
                foreach (var source in config.Sources.OfType<JsonConfigurationSource>())
                {
                    source.ReloadOnChange = false;
                }
            })
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>().ConfigureKestrel(
                server =>
                {
                    var config = server.ApplicationServices.GetRequiredService<AppConfig>();
                    server.ListenAnyIP(config.Ports.Http, o => o.Protocols = HttpProtocols.Http1);
                    server.ListenAnyIP(config.Ports.Http2, o => o.Protocols = HttpProtocols.Http2);
                    server.ListenAnyIP(config.MetricPort, o => o.Protocols = HttpProtocols.Http1);
                }));
}
