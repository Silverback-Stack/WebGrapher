using Auth.WebApi.IdentityProviders;
using Auth.WebApi.IdentityProviders.Auth0;
using Auth.WebApi.IdentityProviders.AzureAD;
using Auth.WebApi.IdentityProviders.Local;
using Auth.WebApi.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Auth.WebApi
{
    public static class AuthSetup
    {
        public static IServiceCollection AddWebApiAuthentication(
            this IServiceCollection services, 
            AuthConfig authConfig)
        {
            ConfigureAuthentication(services, authConfig);

            // Needed for accessing HttpContext.User
            services.AddHttpContextAccessor();

            // Register UserContext as scoped
            services.AddScoped<UserContext>();

            //Add Auth Controllers
            services.AddControllers()
                .AddApplicationPart(typeof(AuthController).Assembly);

            return services;
        }

        private static void ConfigureAuthentication(IServiceCollection services, AuthConfig authConfig)
        {
            switch (authConfig.IdentityProvider.ProviderType)
            {
                case IdentityProviderType.Local:
                    LocalConfiguration.Configure(services, authConfig);
                    services.AddSingleton<IIdentityProvider, LocalAdapter>();
                    break;

                case IdentityProviderType.AzureAD:
                    AzureADConfiguration.Configure(services, authConfig);
                    services.AddSingleton<IIdentityProvider, AzureADAdapter>();
                    break;

                case IdentityProviderType.Auth0:
                    Auth0Configuration.Configure(services, authConfig);
                    services.AddSingleton<IIdentityProvider, Auth0Adapter>();
                    break;

                default:
                    throw new NotSupportedException(
                        $"Identity provider '{authConfig.IdentityProvider.ProviderType}' is not supported.");
            }
        }
    }
}

