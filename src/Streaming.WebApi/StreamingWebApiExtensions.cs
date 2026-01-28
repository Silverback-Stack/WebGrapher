using Auth.WebApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Streaming.Factories;
using Streaming.Infrastructure.Adapters.SignalR;
using System.Text.Json;

namespace Streaming.WebApi
{
    public static class StreamingWebApiExtensions
    {
        public static IServiceCollection AddStreamingWebApi(
            this IServiceCollection services, 
            StreamingConfig streamingConfig,
            StreamingWebApiConfig streamingWebApiSettings,
            AuthConfig authConfig)
        {

            // --- Swagger / OpenAPI ---
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // --- Dependency Injection ---
            services.AddSingleton(streamingWebApiSettings);
            services.AddSingleton(authConfig);

            // --- CORS ---
            services.AddConfiguredCors(streamingWebApiSettings);

            // --- Authentication & Authorization ---
            services.AddWebApiAuthentication(authConfig);
            services.AddAuthorization();

            // --- SignalR ---
            services.AddConfiguredSignalR(streamingConfig);

            return services;
        }

        public static IServiceCollection AddConfiguredCors(
            this IServiceCollection services,
            StreamingWebApiConfig streamingWebApiConfig)
        {
            var allowedOrigins = streamingWebApiConfig.AllowedOrigins ?? Array.Empty<string>();

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
            StreamingConfig streamingConfig)
        {
            ISignalRBuilder signalR;

            // Choose provider
            switch (streamingConfig.Provider)
            {
                case StreamingProvider.SignalRHosted:
                    signalR = services.AddSignalR();
                    break;

                case StreamingProvider.SignalRAzureDefault:
                    signalR = services.AddSignalR()
                        .AddAzureSignalR(streamingConfig.SignalR.AzureDefault.ConnectionString);
                    break;

                case StreamingProvider.SignalRAzureServerless:
                    throw new NotSupportedException("Serverless mode only supported by Function Apps.");

                default:
                    throw new NotSupportedException($"SignalR provider '{streamingConfig.Provider}' is not supported.");
            }

            signalR.AddJsonProtocol(options =>
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
                        path.StartsWithSegments(streamingConfig.SignalR.HubPath))
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
            StreamingConfig streamingConfig,
            StreamingWebApiConfig streamingWebApiConfig)
        {
            ConfigureStreamingPipeline(app, streamingConfig, streamingWebApiConfig);
        }

        // Overload for WebApplication
        public static void UseStreamingWebApi(
            this IApplicationBuilder app,
            StreamingConfig streamingConfig,
            StreamingWebApiConfig streamingWebApiConfig)
        {
            ConfigureStreamingPipeline(app, streamingConfig, streamingWebApiConfig);
        }

        private static void ConfigureStreamingPipeline(
            IApplicationBuilder app,
            StreamingConfig streamingConfig,
            StreamingWebApiConfig streamingWebApiConfig)
        {
            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(streamingWebApiConfig.Swagger.EndpointUrl, streamingWebApiConfig.Swagger.EndpointName);
                options.RoutePrefix = streamingWebApiConfig.Swagger.RoutePrefix;
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<GraphStreamerHub>(streamingConfig.SignalR.HubPath)
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
