using Auth.WebApi.IdentityProviders;
using System;
using System.Security.Claims;

namespace Auth.WebApi.IdentityProviders.AzureAD
{
    public class AzureADAdapter : IIdentityProvider
    {
        private readonly AuthSettings _authSettings;

        public AzureADAdapter(AuthSettings authSettings)
        {
            _authSettings = authSettings;
        }

        public Task<IdentityUser?> ValidateCredentialsAsync(string username, string password)
        {
            // For AzureAD login, we don't validate locally.
            // Throw exception so the middleware sends 401 with metadata with the login url
            throw new UnauthorizedProviderException(GetUnauthorizedResponse());
        }

        public Task<IEnumerable<Claim>> GetClaimsAsync(IdentityUser identityUser)
        {
            // External providers do not issue claims here.
            // JWT middleware supplies them directly from the token.
            throw new NotSupportedException("Claims are provided by the external identity provider JWT.");
        }

        public string GetUserId(ClaimsPrincipal user)
        {

            // Azure AD for statble userId use 'oid' or 'sub' or fallback to 'objectidentifier'
            var userId = user.FindFirst("oid")?.Value
                            ?? user.FindFirst("sub")?.Value
                            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("UserId claim is missing from the token.");

            return userId;
        }

        public UnauthorizedResponse GetUnauthorizedResponse()
        {
            return new UnauthorizedResponse
            {
                IdentityProvider = _authSettings.IdentityProvider.ProviderType.ToString(),
                LoginUrl = _authSettings.IdentityProvider.AzureAD.LoginUrl,
                LogoutUrl = _authSettings.IdentityProvider.AzureAD.LogoutUrl,
                Message = "Unauthorized. Login to authenticate."
            };
        }
    }
}
