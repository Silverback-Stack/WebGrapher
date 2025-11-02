using Auth.WebApi.Auth.IdentityProviders;

namespace Auth.WebApi
{
    public class AuthSettings
    {
        public JwtSettings Jwt { get; set; } = new JwtSettings();
        public IdentityProviderSettings IdentityProvider { get; set; } = new IdentityProviderSettings();
    }
}
