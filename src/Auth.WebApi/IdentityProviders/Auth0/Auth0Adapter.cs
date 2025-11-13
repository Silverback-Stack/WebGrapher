using Auth.WebApi.IdentityProviders;
using System;
using System.Security.Claims;

namespace Auth.WebApi.Auth.IdentityProviders.Auth0
{
    public class Auth0Adapter : IIdentityProvider
    {
        private readonly AuthSettings _authSettings;

        public Auth0Adapter(AuthSettings authSettings)
        {
            _authSettings = authSettings;
        }

        public Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            // For Auth0 login, we don't validate locally.
            // Throw exception so the middleware sends 401 with metadata with the login url
            throw new UnauthorizedProviderException(GetUnauthorizedResponse());
        }

        public Task<IEnumerable<Claim>> GetClaimsAsync(string username)
        {
            // Claims are extracted from the JWT in the controller via [Authorize]
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };
            return Task.FromResult<IEnumerable<Claim>>(claims);
        }

        public string GetUserId(ClaimsPrincipal user)
        {
            // Auth0 uses "sub" for the stable user ID
            var userId = user.FindFirst("sub")?.Value;

            if (userId == null)
                throw new InvalidOperationException("UserId claim is missing from the token.");

            return userId;
        }

        public UnauthorizedResponse GetUnauthorizedResponse()
        {
            return new UnauthorizedResponse
            {
                IdentityProvider = _authSettings.IdentityProvider.ProviderType.ToString(),
                LoginUrl = _authSettings.IdentityProvider.Auth0.LoginUrl,
                LogoutUrl = _authSettings.IdentityProvider.Auth0.LogoutUrl,
                Message = "Unauthorized. Login to authenticate."
            };
        }
    }
}
