using Crawler.Core;
using Events.Core.Bus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;

namespace WebGrapher.Cli.Service.Streaming

//NOTE:
// YOU WILL NEED TO ADD PACKAGES:
// Microsoft.AspNetCore
// Microsoft.AspNetCore.SignalR
// Microsoft.Extensions.Hosting
// Microsoft.Extensions.DependencyInjection

// I!!! IMPORTANT !!!
// THESE STEPS ARE NO LONGER REQUIRED
// PROJECT HAS BEEN REVERTED BACK TO Project Sdk="Microsoft.NET.Sdk
// !!! IMPORTANT !!!
// Need to allow console app to support Web hosting
// Close Visual Studio and edit WebGrapher.Cli.csproj
// CHANGE LINE: <Project Sdk="Microsoft.NET.Sdk">
// TO: <Project Sdk="Microsoft.NET.Sdk.Web">
// THIS MAKES THE Web.SDK avaiable and ConfigureWebHostDefaults option will now become available!

{
    public class StreamingService
    {
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            //Setup Configuration using appsettings overrides
            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Service.Streaming/appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            //bind appsettings overrides to default settings objects
            var streamingSettings = configuration.BindSection<StreamingSettings>("Streaming");
            var signalRSettings = configuration.BindSection<SignalRSettings>("SignalR");

            //Setup Logging
            var logFilePath = $"logs/{streamingSettings.ServiceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);

            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            //Create and Start Streaming Hub
            var (host, hubContext) = await StartHubServerAsync(signalRSettings);
            _host = host;

            StreamerFactory.Create(logger, eventBus, hubContext, streamingSettings);

            logger.LogInformation($"Streaming service started on {signalRSettings.Host}");
        }

        private async static Task<(IHost host, IHubContext<GraphStreamerHub>)> StartHubServerAsync(SignalRSettings signalRSettings)
        {
            // Create and build web host to get hubContext and host SignalR
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSignalR();

                    services.AddCors(options => 
                    {
                        options.AddDefaultPolicy(builder =>
                        {
                            builder
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials()
                                .SetIsOriginAllowed(_ => true); // Allow all origins (or restrict as needed)
                                //.WithOrigins("https://AddressOfUI:PORT")
                        });
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.UseCors();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHub<GraphStreamerHub>(signalRSettings.HubPath);
                            endpoints.MapGet("/", () => "SignalR server is running!");
                        });
                    })
                    .UseUrls(signalRSettings.Host);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    //logging.AddConsole();
                })
                .Build();

            var hubContext = host.Services.GetRequiredService<IHubContext<GraphStreamerHub>>();

            await host.StartAsync();

            return (host, hubContext);
        }

        public static async Task StopHubServerAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                await _host.WaitForShutdownAsync();
                _host.Dispose();
            }
            Log.CloseAndFlush();
        }

    }
}
