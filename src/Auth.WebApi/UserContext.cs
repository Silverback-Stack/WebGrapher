using Auth.WebApi.Auth.IdentityProviders;
using Microsoft.AspNetCore.Http;

namespace Auth.WebApi
{
    public class UserContext
    {
        public string UserId { get; }

        public UserContext(IHttpContextAccessor httpContextAccessor, IIdentityProvider identityProvider)
        {
            var httpContext = httpContextAccessor.HttpContext
                              ?? throw new InvalidOperationException("HttpContext is not available.");

            var user = httpContext.User;

            UserId = identityProvider.GetUserId(user);
        }
    }
}
