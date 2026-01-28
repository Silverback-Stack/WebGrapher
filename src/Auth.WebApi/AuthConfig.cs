using Auth.WebApi.IdentityProviders;

namespace Auth.WebApi
{
    public class AuthConfig
    {
        public JwtSettings Jwt { get; set; } = new JwtSettings();
        public IdentityProviderSettings IdentityProvider { get; set; } = new IdentityProviderSettings();
    }
}
