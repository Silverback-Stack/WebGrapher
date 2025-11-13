using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Auth.WebApi.Auth.IdentityProviders.AzureAD
{
    public static class AzureADConfiguration
    {
        public static void Configure(IServiceCollection services, AuthSettings settings)
        {
            var azure = settings.IdentityProvider.AzureAD;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{azure.Instance}{azure.TenantId}/v2.0";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidAudiences = new[]
                            {
                                azure.Audience,
                                azure.ClientId
                            },
                        ValidIssuers = new[]
                        {
                            $"https://sts.windows.net/{azure.TenantId}/", // v1 issuer
                            $"https://login.microsoftonline.com/{azure.TenantId}/v2.0" // v2 issuer
                        },
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };

                    // Custom 401 response
                    options.Events = UnauthorizedHandlerFactory.Create(settings);

                    options.RequireHttpsMetadata = true;
                });
        }
    }
}
