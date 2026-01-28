using Auth.WebApi;
using Graphing.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Graphing.WebApi
{
    public static class GraphingWebApiExtensions
    {
        public static IServiceCollection AddGraphingWebApi(
            this IServiceCollection services, 
            IPageGrapher pageGrapher, 
            GraphingWebApiConfig graphingWebApiConfig,
            AuthConfig authConfig)
        {
            // --- Controllers ---
            services.AddControllers()
                    .AddApplicationPart(typeof(GraphingWebApiExtensions).Assembly);

            // --- Swagger / OpenAPI ---
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // --- Dependency Injection ---
            services.AddSingleton(graphingWebApiConfig);
            services.AddSingleton(authConfig);
            services.AddSingleton<IPageGrapher>(pageGrapher);

            // --- CORS ---
            services.AddConfiguredCors(graphingWebApiConfig);

            // --- Authentication ---
            services.AddWebApiAuthentication(authConfig);

            // --- Proxy for images that dont support CORS ---
            services.AddHttpClient("ProxyClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(graphingWebApiConfig.ProxyClientTimeOutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                //  For images (JPEG, PNG, GIF), servers already deliver them in a compressed format
                //  optimized for the web. Trying to decompress them in the client wastes CPU and memory.
                AutomaticDecompression = DecompressionMethods.None
            });

            return services;
        }

        public static IServiceCollection AddConfiguredCors(
            this IServiceCollection services,
            GraphingWebApiConfig graphingWebApiConfig)
        {
            var allowedOrigins = graphingWebApiConfig.AllowedOrigins ?? Array.Empty<string>();

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

        public static void UseGraphingWebApi(
            this WebApplication app, 
            GraphingWebApiConfig graphingWebApiConfig)
        {
            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(graphingWebApiConfig.Swagger.EndpointUrl, graphingWebApiConfig.Swagger.EndpointName);
                options.RoutePrefix = graphingWebApiConfig.Swagger.RoutePrefix;
            });

            app.MapControllers();
        }

    }
}

