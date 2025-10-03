using System;
using Events.Core.Bus;
using Graphing.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Settings.Core;
using WebGrapher.Cli.Service.Graphing.Controllers;

namespace WebGrapher.Cli.Service.Graphing
{
    internal class GraphingService
    {
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            //Setup Configuration using appsettings overrides
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Graphing");

            //bind appsettings overrides to default settings objects
            var webApiSettings = configuration.BindSection<WebApiSettings>("WebApi");
            var graphingSettings = configuration.BindSection<GraphingSettings>("Graphing");

            // Setup Serilog Logging
            var serilogLogger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(
                    path: $"logs/{graphingSettings.ServiceName}.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();

            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();

            //Create PageGrapher service
            var pageGrapher = GraphingFactory.Create(logger, eventBus, graphingSettings);


            //Start WEB.API:
            var host = await StartWebApiServerAsync(pageGrapher, webApiSettings);
            _host = host;

            logger.LogInformation("{ServiceName} service started on {Host}/{Swagger}",
                graphingSettings.ServiceName, 
                webApiSettings.Host,
                webApiSettings.SwaggerRoutePrefix);
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
