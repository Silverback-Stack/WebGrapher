using System;
using Caching.Core;
using Crawler.Core;
using Events.Core.Bus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using WebMapper.Cli.Service.Crawler.Controllers;

namespace WebMapper.Cli.Service.Crawler
{
    internal class CrawlerService
    {
        private const string HOST = "http://localhost:5000";
        private const string SWAGGER_ENDPOINT_URL = "/swagger/v1/swagger.json";
        private const string SWAGGER_ENDPOINT_NAME = "Crawler API V1";
        private const string SWAGGER_ROUTE_PREFIX = "swagger";
        
        private static IHost? _host;

        public async static Task<IPageCrawler> InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(CrawlerService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Debug)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);
            var logger = loggerFactory.CreateLogger<IPageCrawler>();

            var host = await StartWebApiServerAsync(eventBus);
            _host = host;

            var cache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InMemory,
                logger);

            var requestSender = RequestFactory.CreateRequestSender(
                logger, cache);

            var sitePolicyResolver = new SitePolicyResolver(requestSender);

            logger.LogInformation($"The Crawler API started: {HOST}/{SWAGGER_ROUTE_PREFIX}");

            return CrawlerFactory.CreateCrawler(
                logger, eventBus, cache, requestSender, sitePolicyResolver);
        }

        private async static Task<IHost> StartWebApiServerAsync(IEventBus eventBus)
        {
            var controllersToKeep = new[] {
                typeof(CrawlerController).Assembly
            };

            // Create and build web host to get hubContext and host SignalR
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddControllers()
                        .ConfigureApplicationPartManager(apm =>
                        {
                            // List only the controllers you want for this service:
                            apm.ApplicationParts.Clear();
                            foreach (var assembly in controllersToKeep)
                                apm.ApplicationParts.Add(new AssemblyPart(assembly));
                        });

                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen();

                    // add event bus to DI for use in controllers
                    services.AddSingleton<IEventBus>(eventBus);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseSwagger();
                        app.UseSwaggerUI(options =>
                        {
                            options.SwaggerEndpoint(SWAGGER_ENDPOINT_URL, SWAGGER_ENDPOINT_NAME);
                            options.RoutePrefix = SWAGGER_ROUTE_PREFIX;
                        });
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapGet("/", () => "Web API server is running.");
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

            await host.StartAsync();

            return host;
        }

        public static async Task StopWebApiServerAsync()
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
