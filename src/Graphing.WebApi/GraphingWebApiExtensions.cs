using Auth.WebApi;
using Graphing.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Graphing.WebApi
{
    public static class GraphingWebApiExtensions
    {
        public static IServiceCollection AddGraphingWebApi(
            this IServiceCollection services, 
            IPageGrapher pageGrapher, 
            GraphingWebApiSettings graphingWebApiSettings,
            AuthSettings authSettings)
        {
            // --- Controllers ---
            services.AddControllers()
                    .AddApplicationPart(typeof(GraphingWebApiExtensions).Assembly);

            // --- Swagger / OpenAPI ---
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // --- Dependency Injection ---
            services.AddSingleton(graphingWebApiSettings);
            services.AddSingleton(authSettings);
            services.AddSingleton<IPageGrapher>(pageGrapher);

            // --- CORS ---
            services.AddConfiguredCors(graphingWebApiSettings);

            // --- Authentication ---
            services.AddWebApiAuthentication(authSettings);

            return services;
        }

        public static IServiceCollection AddConfiguredCors(
            this IServiceCollection services,
            GraphingWebApiSettings graphingWebApiSettings)
        {
            var allowedOrigins = graphingWebApiSettings.AllowedOrigins ?? Array.Empty<string>();

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
            GraphingWebApiSettings graphingWebApiSettings)
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
                options.SwaggerEndpoint(graphingWebApiSettings.Swagger.EndpointUrl, graphingWebApiSettings.Swagger.EndpointName);
                options.RoutePrefix = graphingWebApiSettings.Swagger.RoutePrefix;
            });

            app.MapControllers();
        }

    }
}

