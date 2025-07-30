using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace WebMapper.Cli

//NOTE:
// YOU WILL NEED TO ADD PACKAGES:
// Microsoft.AspNetCore
// Microsoft.AspNetCore.SignalR
// Microsoft.Extensions.Hosting
// Microsoft.Extensions.DependencyInjection

// NOTE:
// Need to allow console app to support Web hosting
// Close Visual Studio and edit WebMapper.Cli.csproj
// CHANGE LINE: <Project Sdk="Microsoft.NET.Sdk">
// TO: <Project Sdk="Microsoft.NET.Sdk.Web">
// THIS MAKES THE Web.SDK avaiable and ConfigureWebHostDefaults option will now become available!

{
    public class StreamingService
    {

        private const string DEFAULT_STREAMING_HOST = "http://localhost:5000";

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(StreamingService).Name;

            var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/streaming.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

            var logger = Logging.Core.LoggerFactory.CreateLogger(
                serviceName,
                LoggerOptions.Serilog,
                loggerConfig
            );



            string[] args = { };

            // Create and build web host to get hubContext and host SignalR
            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHub<GraphHub>("/graphhub");
                            endpoints.MapGet("/", () => "SignalR server is running!");
                        });
                    })
                    .UseUrls(DEFAULT_STREAMING_HOST);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    //logging.AddConsole();
                })
                .Build();

            // Resolve IHubContext from the container:
            var hubContext = host.Services.GetRequiredService<IHubContext<GraphHub>>();


            StreamerFactory.Create(logger, eventBus, hubContext);

            logger.LogInformation($"{serviceName} is listening on {DEFAULT_STREAMING_HOST}");

            // Run the host so the hub is live:
            await host.RunAsync();
        }

    }
}
