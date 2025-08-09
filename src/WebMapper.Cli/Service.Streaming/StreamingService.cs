using Events.Core.Bus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);

            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            var (host, hubContext) = await StartHubServerAsync();
            _host = host;

            StreamerFactory.Create(logger, eventBus, hubContext);

            logger.LogInformation($"Streaming service started on {HOST}");
        }

        private async static Task<(IHost host, IHubContext<GraphStreamerHub>)> StartHubServerAsync()
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
                            endpoints.MapHub<GraphStreamerHub>("/graphStreamerHub");
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
