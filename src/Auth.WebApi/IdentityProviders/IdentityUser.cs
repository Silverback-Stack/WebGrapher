using System;

namespace Auth.WebApi.IdentityProviders
{
    /// <summary>
    /// Provider-Agnostic Idenity User.
    /// </summary>
    public class IdentityUser
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
