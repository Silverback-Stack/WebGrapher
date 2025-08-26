using System;
using Events.Core.Bus;
using Graphing.Core;
using Graphing.Core.WebGraph.Adapters.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using WebGrapher.Cli.Service.Graphing.Controllers;

namespace WebGrapher.Cli.Service.Graphing
{
    internal class GraphingService
    {
        private const string HOST = "http://localhost:5000";
        private const string SWAGGER_ENDPOINT_URL = "/swagger/v1/swagger.json";
        private const string SWAGGER_ENDPOINT_NAME = "Grapher API V1";
        private const string SWAGGER_ROUTE_PREFIX = "swagger";

        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            //SETUP LOGGING:
            var serviceName = typeof(GraphingService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Debug)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();

            //SETUP WEBGRAPH:
            var webGraph = new InMemoryWebGraphAdapter(logger);

            //CREATE SERVICE:
            var pageGrapher = GraphingFactory.Create(logger, eventBus, webGraph);

            //START WEB API:
            var host = await StartWebApiServerAsync(pageGrapher);
            _host = host;

            logger.LogInformation($"Graphing service started on {HOST}/{SWAGGER_ROUTE_PREFIX}");
        }

        private async static Task<IHost> StartWebApiServerAsync(IPageGrapher pageGrapher)
        {
            // Create and build web host
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddControllers()
                        .AddApplicationPart(typeof(GraphController).Assembly);
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen();

                    // Flexible CORS for any localhost port
                    services.AddCors(options =>
                    {
                        options.AddPolicy("AllowLocalhost", policy =>
                        {
                            policy.SetIsOriginAllowed(origin =>
                            {
                                // Allow all http/https localhost origins
                                return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                                       (uri.Host == "localhost" || uri.Host == "127.0.0.1");
                            })
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                        });
                    });

                    // dependency injection:
                    services.AddSingleton<IPageGrapher>(pageGrapher);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseCors("AllowLocalhost");

                        app.UseSwagger();
                        app.UseSwaggerUI(options =>
                        {
                            options.SwaggerEndpoint(SWAGGER_ENDPOINT_URL, SWAGGER_ENDPOINT_NAME);
                            options.RoutePrefix = SWAGGER_ROUTE_PREFIX;
                        });
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
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
