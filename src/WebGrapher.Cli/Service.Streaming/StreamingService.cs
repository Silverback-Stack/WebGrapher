using System;
using Events.Core.Bus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Settings.Core;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;
using WebGrapher.Cli.Service.Graphing;


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
            //Setup Configuration using appsettings overrides
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Streaming");

            //Bind to strongly typed objects
            var streamingSettings = configuration.BindSection<StreamingSettings>("Streaming");

            // Setup Serilog Logging
            var serilogLogger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(
                    path: $"logs/{streamingSettings.ServiceName}.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();

            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            //Create and Start Streaming Hub
            var (host, hubContext, hubUrl) = await StartHubServerAsync(streamingSettings);
            _host = host;

            StreamerFactory.Create(logger, eventBus, hubContext, streamingSettings);

            logger.LogInformation("{ServiceName} service started on {HubUrl}",
                streamingSettings.ServiceName,
                hubUrl);
        }

        private async static Task<(IHost host, IHubContext<GraphStreamerHub>, string hubUrl)> StartHubServerAsync(StreamingSettings streamingSettings)
        {
            var hubUrl = "";
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

                    switch (streamingSettings.Provider)
                    {
                        case StreamingProvider.HostedSignalR:
                            services.AddSignalR();
                            break;

                        case StreamingProvider.AzureSignalRDefault:
                            services.AddSignalR()
                                .AddAzureSignalR(streamingSettings.AzureSignalRDefault.ConnectionString);
                            break;

                        case StreamingProvider.AzureSignalRServerless:
                            throw new NotSupportedException("Serverless mode only supported by serverless architecture such as Function apps.");

                        default:
                            throw new NotSupportedException($"SignalR mode '{streamingSettings.Provider}' is not supported.");
                    }
                });

                webBuilder.Configure(app =>
                {
                    app.UseCors();
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<GraphStreamerHub>(streamingSettings.HubPath);
                        endpoints.MapGet("/", () => "SignalR streaming service is running.");
                    });
                });

                switch (streamingSettings.Provider)
                {
                    case StreamingProvider.HostedSignalR:
                        //bind explicit URL and PORT
                        webBuilder.UseUrls(streamingSettings.HostedSignaR.Host);
                        hubUrl = $"{streamingSettings.HostedSignaR.Host}{streamingSettings.HubPath}";
                        break;

                    case StreamingProvider.AzureSignalRDefault:
                        var isProduction = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.ToLower() == "production";
                        if (!isProduction)
                        {
                            //Bind explicit URL and PORT for local testing
                            webBuilder.UseUrls(streamingSettings.HostedSignaR.Host);
                            hubUrl = $"{streamingSettings.HostedSignaR.Host}{streamingSettings.HubPath}";
                        }
                        else
                        {
                            //Bind to random free port for production.
                            //Azure handless connections and port when deployed to Azure
                            webBuilder.UseUrls("http://127.0.0.1:0");
                            hubUrl = $"{streamingSettings.AzureSignalRDefault.Endpoint}{streamingSettings.HubPath}";
                        }
                        break;

                    case StreamingProvider.AzureSignalRServerless:
                        hubUrl = $"{streamingSettings.AzureSignalRServerless.Endpoint}{streamingSettings.HubPath}";
                        break;

                    default:
                        throw new NotSupportedException($"SignalR mode '{streamingSettings.Provider}' is not supported.");
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
