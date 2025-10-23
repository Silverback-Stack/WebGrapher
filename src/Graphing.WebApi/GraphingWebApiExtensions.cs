using Graphing.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Graphing.WebApi
{
    public static class GraphingWebApiExtensions
    {
        public static IServiceCollection AddGraphingWebApi(this IServiceCollection services, IPageGrapher pageGrapher)
        {
            services.AddControllers()
                    .AddApplicationPart(typeof(GraphingWebApiExtensions).Assembly);

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
                            return uri.IsLoopback; // true for 127.0.0.1 or localhost
                        return false;
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            // Register Page Grapher as a singleton for Dependency Injection
            services.AddSingleton<IPageGrapher>(pageGrapher);

            return services;
        }

        public static void UseGraphingWebApi(this WebApplication app, WebApiSettings settings)
        {
            app.UseRouting();
            app.UseCors("AllowLocalhost"); //must come before static files and swagger
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(settings.SwaggerEndpointUrl, settings.SwaggerEndpointName);
                options.RoutePrefix = settings.SwaggerRoutePrefix;
            });

            app.MapControllers();
        }
    }
}
