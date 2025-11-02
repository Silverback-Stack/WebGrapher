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
                    options.Audience = settings.Jwt.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidAudiences = new[] { azure.ClientId, settings.Jwt.Audience }
                    };

                    // Custom 401 response
                    options.Events = UnauthorizedHandlerFactory.Create(settings);

                    options.RequireHttpsMetadata = true; //disable this to test locally without https
                });
        }
    }
}
