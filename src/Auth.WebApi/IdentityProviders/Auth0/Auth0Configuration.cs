using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Auth.WebApi.IdentityProviders.Auth0
{
    public static class Auth0Configuration
    {
        public static void Configure(IServiceCollection services, AuthSettings settings)
        {
            var auth0 = settings.IdentityProvider.Auth0;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"https://{auth0.Domain}/";
                    options.Audience = auth0.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30) //account for small timing differences between client and server
                    };

                    // Custom 401 response
                    options.Events = UnauthorizedHandlerFactory.Create(settings);
                });
        }
    }
}
