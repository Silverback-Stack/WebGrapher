using Auth.WebApi.Auth.IdentityProviders;
using Auth.WebApi.Auth.IdentityProviders.AppSettings;
using Auth.WebApi.Auth.IdentityProviders.Auth0;
using Auth.WebApi.Auth.IdentityProviders.AzureAD;
using Auth.WebApi.Auth.IdentityProviders.Local;
using Auth.WebApi.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Auth.WebApi
{
    public static class AuthSetup
    {
        public static IServiceCollection AddWebApiAuthentication(
            this IServiceCollection services, 
            AuthSettings authSettings)
        {
            ConfigureAuthentication(services, authSettings);

            //Add Auth Controllers
            services.AddControllers()
                .AddApplicationPart(typeof(AuthController).Assembly);

            return services;
        }

        private static void ConfigureAuthentication(IServiceCollection services, AuthSettings authSettings)
        {
            switch (authSettings.IdentityProvider.ProviderType)
            {
                case IdentityProviderType.Local:
                    LocalConfiguration.Configure(services, authSettings);
                    services.AddSingleton<IIdentityProvider, LocalAdapter>();
                    break;

                case IdentityProviderType.AzureAD:
                    AzureADConfiguration.Configure(services, authSettings);
                    services.AddSingleton<IIdentityProvider, AzureADAdapter>();
                    break;

                case IdentityProviderType.Auth0:
                    Auth0Configuration.Configure(services, authSettings);
                    services.AddSingleton<IIdentityProvider, Auth0Adapter>();
                    break;

                default:
                    throw new NotSupportedException(
                        $"Identity provider '{authSettings.IdentityProvider.ProviderType}' is not supported.");
            }
        }
    }
}

