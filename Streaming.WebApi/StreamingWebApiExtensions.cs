using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;

namespace Streaming.WebApi
{
    public static class StreamingWebApiExtensions
    {
        public static IServiceCollection AddStreamingWebApi(this IServiceCollection services, StreamingSettings settings)
        {
            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true);
                });
            });

            // Add SignalR
            switch (settings.Provider)
            {
                case StreamingProvider.HostedSignalR:
                    services.AddSignalR();
                    break;

                case StreamingProvider.AzureSignalRDefault:
                    services.AddSignalR()
                        .AddAzureSignalR(settings.AzureSignalRDefault.ConnectionString);
                    break;

                case StreamingProvider.AzureSignalRServerless:
                    throw new NotSupportedException("Serverless mode only supported by Function Apps.");

                default:
                    throw new NotSupportedException($"SignalR provider '{settings.Provider}' is not supported.");
            }

            return services;
        }

        // Overload for IApplicationBuilder
        public static void UseStreamingWebApi(this WebApplication app, StreamingSettings settings)
        {
            ConfigureStreamingEndpoints(app, settings);
        }

        // Overload for WebApplication
        public static void UseStreamingWebApi(this IApplicationBuilder app, StreamingSettings settings)
        {
            ConfigureStreamingEndpoints(app, settings);
        }

        private static void ConfigureStreamingEndpoints(IApplicationBuilder app, StreamingSettings settings)
        {
            app.UseRouting();
            app.UseCors("AllowAll");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<GraphStreamerHub>(settings.HubPath);
                endpoints.MapGet("/", async ctx =>
                {
                    await ctx.Response.WriteAsync("SignalR Streaming Service is running.");
                });
            });
        }
    }
}
