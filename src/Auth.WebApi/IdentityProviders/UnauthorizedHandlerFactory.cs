using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Auth.WebApi.IdentityProviders
{
    public class UnauthorizedHandlerFactory
    {
        public static JwtBearerEvents Create(AuthSettings settings)
        {
            return new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    // Resolve the adapter for the current provider
                    var adapter = context.HttpContext.RequestServices.GetRequiredService<IIdentityProvider>();

                    // Get the login metadata from the adapter
                    var response = adapter.GetUnauthorizedResponse();

                    // Send to client
                    await context.Response.WriteAsJsonAsync(response);
                }
            };
        }
    }
}
