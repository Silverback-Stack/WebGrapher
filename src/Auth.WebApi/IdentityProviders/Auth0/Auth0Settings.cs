using System;

namespace Auth.WebApi.IdentityProviders.Auth0
{
    public class Auth0Settings
    {
        public string Domain { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string LogoutUrl { get; set; } = string.Empty;
    }
}
