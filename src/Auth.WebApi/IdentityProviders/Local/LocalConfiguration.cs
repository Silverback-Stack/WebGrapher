using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Auth.WebApi.Auth.IdentityProviders.Local
{
    public static class LocalConfiguration
    {
        public static void Configure(IServiceCollection services, AuthSettings settings)
        {
            var jwt = settings.Jwt;
            var key = Encoding.UTF8.GetBytes(jwt.Key);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                // Custom 401 response
                options.Events = UnauthorizedHandlerFactory.Create(settings);
            });
        }
    }
}
