using System;
using Events.Core.Bus;
using Graphing.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            //Setup Configuration using appsettings overrides
            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Service.Graphing/appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"Service.Graphing/appsettings.{environment}.json", optional: true, reloadOnChange: true) // local overrides
            .AddEnvironmentVariables()
            .Build();

            //bind appsettings overrides to default settings objects
            var webApiSettings = configuration.BindSection<WebApiSettings>("WebApi");
            var graphingSettings = configuration.BindSection<GraphingSettings>("Graphing");


            //Setup Logging
            var logFilePath = $"logs/{graphingSettings.ServiceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();

            //Create PageGrapher service
            var pageGrapher = GraphingFactory.Create(logger, eventBus, graphingSettings);


            //Start WEB.API:
            var host = await StartWebApiServerAsync(pageGrapher, webApiSettings);
            _host = host;

            logger.LogInformation($"Graphing service started on {webApiSettings.Host}/{webApiSettings.SwaggerRoutePrefix}");
        }

        private async static Task<IHost> StartWebApiServerAsync(IPageGrapher pageGrapher, WebApiSettings webApiSettings)
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
                                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                                {
                                    return uri.IsLoopback; // true for 127.0.0.1 or localhost
                                }
                                return false;
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
                    webBuilder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "Service.Graphing", "wwwroot"));

                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseCors("AllowLocalhost"); //must come before static files and swagger

                        app.UseDefaultFiles();
                        app.UseStaticFiles();
                                                
                        app.UseSwagger();
                        app.UseSwaggerUI(options =>
                        {
                            options.SwaggerEndpoint(webApiSettings.SwaggerEndpointUrl, webApiSettings.SwaggerEndpointName);
                            options.RoutePrefix = webApiSettings.SwaggerRoutePrefix;
                        });
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    })
                    .UseUrls(webApiSettings.Host);
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
