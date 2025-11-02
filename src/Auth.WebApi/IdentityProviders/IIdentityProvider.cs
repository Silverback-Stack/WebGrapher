using System;
using System.Security.Claims;

namespace Auth.WebApi.Auth.IdentityProviders
{
    public interface IIdentityProvider
    {
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task<IEnumerable<Claim>> GetClaimsAsync(string username);
        UnauthorizedResponse GetUnauthorizedResponse();
    }
}
