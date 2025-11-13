using Auth.WebApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;
using System.Text.Json;

namespace Streaming.WebApi
{
    public static class StreamingWebApiExtensions
    {
        public static IServiceCollection AddStreamingWebApi(
            this IServiceCollection services, 
            StreamingSettings streamingSettings,
            StreamingWebApiSettings streamingWenApiSettings,
            AuthSettings authSettings)
        {

            // --- Swagger / OpenAPI ---
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // --- Dependency Injection ---
            services.AddSingleton(streamingWenApiSettings);
            services.AddSingleton(authSettings);

            // --- CORS ---
            services.AddConfiguredCors(streamingWenApiSettings);

            // --- Authentication & Authorization ---
            services.AddWebApiAuthentication(authSettings);
            services.AddAuthorization();

            // --- SignalR ---
            services.AddConfiguredSignalR(streamingSettings);

            return services;
        }

        public static IServiceCollection AddConfiguredCors(
            this IServiceCollection services,
            StreamingWebApiSettings streamingWebApiSettings)
        {
            var allowedOrigins = streamingWebApiSettings.AllowedOrigins ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                    {
                        // Try to parse the incoming origin string into a valid absolute URI.
                        // If it's not a valid absolute URI (e.g., malformed or relative), reject it by returning false.
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                            return false;

                        // Allow localhost or any configured origin in app settings
                        return uri.IsLoopback ||
                               allowedOrigins.Contains(uri.GetLeftPart(UriPartial.Authority));
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            return services;
        }


        public static IServiceCollection AddConfiguredSignalR(
            this IServiceCollection services,
            StreamingSettings streamingSettings)
        {
            // Choose provider
            switch (streamingSettings.SignalR.Provider)
            {
                case StreamingProvider.HostedSignalR:
                    services.AddSignalR();
                    break;

                case StreamingProvider.AzureSignalRDefault:
                    services.AddSignalR()
                        .AddAzureSignalR(streamingSettings.SignalR.AzureSignalRDefault.ConnectionString);
                    break;

                case StreamingProvider.AzureSignalRServerless:
                    throw new NotSupportedException("Serverless mode only supported by Function Apps.");

                default:
                    throw new NotSupportedException($"SignalR provider '{streamingSettings.SignalR.Provider}' is not supported.");
            }

            services.AddSignalR().AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });

            // Allow JWT from query string (for browser clients)
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Events ??= new JwtBearerEvents();

                var previousHandler = options.Events.OnMessageReceived;

                options.Events.OnMessageReceived = async context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    // Allow token from query string for SignalR hub connections
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments(streamingSettings.HubPath))
                    {
                        context.Token = accessToken;
                    }

                    // Preserve any existing handler logic
                    if (previousHandler is not null)
                        await previousHandler(context);
                };
            });

            return services;
        }


        // --- Pipeline setup ---
        public static void UseStreamingWebApi(
            this WebApplication app, 
            StreamingSettings streamingSettings,
            StreamingWebApiSettings streamingWebApiSettings)
        {
            ConfigureStreamingPipeline(app, streamingSettings, streamingWebApiSettings);
        }

        // Overload for WebApplication
        public static void UseStreamingWebApi(
            this IApplicationBuilder app, 
            StreamingSettings streamingSettings,
            StreamingWebApiSettings streamingWebApiSettings)
        {
            ConfigureStreamingPipeline(app, streamingSettings, streamingWebApiSettings);
        }

        private static void ConfigureStreamingPipeline(
            IApplicationBuilder app, 
            StreamingSettings streamingSettings,
            StreamingWebApiSettings streamingWebApiSettings)
        {
            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(streamingWebApiSettings.Swagger.EndpointUrl, streamingWebApiSettings.Swagger.EndpointName);
                options.RoutePrefix = streamingWebApiSettings.Swagger.RoutePrefix;
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<GraphStreamerHub>(streamingSettings.HubPath)
                    .RequireAuthorization();


                endpoints.MapControllers();

                endpoints.MapGet("/", async ctx =>
                {
                    await ctx.Response.WriteAsync("SignalR Streaming Service is running.");
                });
            });
        }
    }
}
