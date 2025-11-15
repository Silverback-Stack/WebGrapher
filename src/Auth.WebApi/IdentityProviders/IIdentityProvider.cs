using System;
using System.Security.Claims;

namespace Auth.WebApi.IdentityProviders
{
    public interface IIdentityProvider
    {
        Task<IdentityUser?> ValidateCredentialsAsync(string username, string password);
        Task<IEnumerable<Claim>> GetClaimsAsync(IdentityUser identityUser);
        string GetUserId(ClaimsPrincipal user);
        UnauthorizedResponse GetUnauthorizedResponse();
    }
}
