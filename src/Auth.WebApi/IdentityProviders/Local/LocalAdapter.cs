using System;
using System.Security.Claims;

namespace Auth.WebApi.IdentityProviders.Local
{
    public class LocalAdapter : IIdentityProvider
    {
        private readonly AuthConfig _authConfig;

        public LocalAdapter(AuthConfig authConfig)
        {
            _authConfig = authConfig;
        }

        public Task<IdentityUser?> ValidateCredentialsAsync(string username, string password)
        {
            var localUser = _authConfig.IdentityProvider.Local.Users
                .FirstOrDefault(u => u.Username == username && u.Password == password);

            if (localUser == null)
                return Task.FromResult<IdentityUser?>(null);

            //map local User to Provider-Agnostic IdentityUser
            var user = new IdentityUser
            {
                UserId = localUser.Id.ToString(),
                Username = localUser.Username,
                Roles = localUser.Roles
            };

            return Task.FromResult<IdentityUser?>(user);
        }


        public Task<IEnumerable<Claim>> GetClaimsAsync(IdentityUser identityUser)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, identityUser.UserId),
                new Claim(ClaimTypes.Name, identityUser.Username)
            };

            foreach (var role in identityUser.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            return Task.FromResult<IEnumerable<Claim>>(claims);
        }

        public string GetUserId(ClaimsPrincipal user)
        {
            // We use UserId as the user ID
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("The NameIdentifier (UserId) claim is missing.");

            return userId;
        }

        public UnauthorizedResponse GetUnauthorizedResponse()
        {
            // Local login happens via the API itself, so no external login URL
            return new UnauthorizedResponse
            {
                IdentityProvider = _authConfig.IdentityProvider.ProviderType.ToString(),
                LoginUrl = string.Empty, // the client app knows to show local login form
                LogoutUrl = string.Empty, // the client app knows to show local logout page
                Message = "Unauthorized. Login to authenticate."
            };
        }
    }
}
