using Events.Core.Bus;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;

namespace WebMapper.Cli.Service.Streaming

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
        private const string HOST = "http://localhost:5001";
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(StreamingService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);

            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            var (host, hubContext) = await StartHubServerAsync();
            _host = host;

            StreamerFactory.Create(logger, eventBus, hubContext);

            logger.LogInformation($"The Streaming Hub started: {HOST}");
        }

        private async static Task<(IHost host, IHubContext<GraphHub>)> StartHubServerAsync()
        {
            // Create and build web host to get hubContext and host SignalR
            var host = Host.CreateDefaultBuilder()
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
                    .UseUrls(HOST);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    //logging.AddConsole();
                })
                .Build();

            var hubContext = host.Services.GetRequiredService<IHubContext<GraphHub>>();

            await host.StartAsync();

            return (host, hubContext);
        }

        public static async Task StopHubServerAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            Log.CloseAndFlush();
        }

    }
}
