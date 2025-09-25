using System;
using Events.Core.Bus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;


//NOTE:
// YOU WILL NEED TO ADD PACKAGES:
// Microsoft.AspNetCore
// Microsoft.AspNetCore.SignalR
// Microsoft.Extensions.Hosting
// Microsoft.Extensions.DependencyInjection
// Microsoft.Azure.SignalR

namespace WebGrapher.Cli.Service.Streaming
{
    public class StreamingService
    {
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            //Setup Configuration using appsettings overrides
            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Service.Streaming/appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"Service.Streaming/appsettings.{environment}.json", optional: true, reloadOnChange: true) // local overrides
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
            var (host, hubContext, hubUrl) = await StartHubServerAsync(signalRSettings);
            _host = host;

            StreamerFactory.Create(logger, eventBus, hubContext, streamingSettings);

            logger.LogInformation($"Streaming service started on {hubUrl}");
        }

        private async static Task<(IHost host, IHubContext<GraphStreamerHub>, string hubUrl)> StartHubServerAsync(SignalRSettings signalRSettings)
        {
            string hubUrl = signalRSettings.Hosted.Host;
            var hostBuilder = Host.CreateDefaultBuilder();

            hostBuilder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                //logging.AddConsole();
            });

            hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services =>
                {
                    services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(builder =>
                        {
                            builder
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials()
                                .SetIsOriginAllowed(_ => true);
                        });
                    });

                    switch (signalRSettings.Service)
                    {
                        case ServiceType.Hosted:
                            services.AddSignalR();
                            break;

                        case ServiceType.AzureDefault:
                            services.AddSignalR()
                                .AddAzureSignalR(signalRSettings.Azure.ConnectionString);
                            break;

                        case ServiceType.AzureServerless:
                            throw new NotSupportedException("Serverless mode only supported by serverless architecture such as Function apps.");

                        default:
                            throw new NotSupportedException($"SignalR mode '{signalRSettings.Service}' is not supported.");
                    }
                });

                webBuilder.Configure(app =>
                {
                    app.UseCors();
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<GraphStreamerHub>(signalRSettings.HubPath);
                        endpoints.MapGet("/", () => "SignalR streaming service is running.");
                    });
                });

                switch (signalRSettings.Service)
                {
                    case ServiceType.Hosted:
                        //bind explicit URL and PORT
                        webBuilder.UseUrls(signalRSettings.Hosted.Host);
                        break;

                    case ServiceType.AzureDefault:
                        var isProduction = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.ToLower() == "production";
                        if (!isProduction)
                        {
                            //Bind explicit URL and PORT for local testing
                            webBuilder.UseUrls(signalRSettings.Hosted.Host);
                        }
                        else
                        {
                            //Bind to random free port for production.
                            //Azure handless connections and port when deployed to Azure
                            webBuilder.UseUrls("http://127.0.0.1:0");
                            hubUrl = $"{signalRSettings.Azure.Endpoint}{signalRSettings.HubPath}";
                        }
                        break;

                    case ServiceType.AzureServerless:
                        hubUrl = $"{signalRSettings.Azure.Endpoint}{signalRSettings.HubPath}";
                        break;

                    default:
                        throw new NotSupportedException($"SignalR mode '{signalRSettings.Service}' is not supported.");
                }
            });

            var host = hostBuilder.Build();
            var hubContext = host.Services.GetRequiredService<IHubContext<GraphStreamerHub>>();

            await host.StartAsync();

            return (host, hubContext, hubUrl);
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
