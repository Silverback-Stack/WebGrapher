using System;
using System.Security.Claims;

namespace Auth.WebApi.Auth.IdentityProviders.AppSettings
{
    public class LocalAdapter : IIdentityProvider
    {
        private readonly AuthSettings _authSettings;

        public LocalAdapter(AuthSettings authSettings)
        {
            _authSettings = authSettings;
        }

        public Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            bool valid = _authSettings.IdentityProvider.Local.Users
                .Any(u => u.Username == username && u.Password == password);

            return Task.FromResult(valid);
        }


        public Task<IEnumerable<Claim>> GetClaimsAsync(string username)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };

            return Task.FromResult<IEnumerable<Claim>>(claims);
        }

        public string GetUserId(ClaimsPrincipal user)
        {
            // We use Username as the user ID
            var userId = user.FindFirst(ClaimTypes.Name)?.Value;

            if (userId == null)
                throw new InvalidOperationException("UserId claim is missing from the token.");

            return userId;
        }

        public UnauthorizedResponse GetUnauthorizedResponse()
        {
            // Local login happens via the API itself, so no external login URL
            return new UnauthorizedResponse
            {
                IdentityProvider = _authSettings.IdentityProvider.ProviderType.ToString(),
                LoginUrl = string.Empty, // the client app knows to show local login form
                LogoutUrl = string.Empty, // the client app knows to show local logout page
                Message = "Unauthorized. Login to authenticate."
            };
        }
    }
}
