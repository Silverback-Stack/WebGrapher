using System;

namespace Auth.WebApi.Auth.IdentityProviders
{
    public class UnauthorizedResponse
    {
        public string IdentityProvider { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
