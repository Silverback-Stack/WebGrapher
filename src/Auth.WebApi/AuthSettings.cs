using Auth.WebApi.IdentityProviders;

namespace Auth.WebApi
{
    public class AuthSettings
    {
        public JwtSettings Jwt { get; set; } = new JwtSettings();
        public IdentityProviderSettings IdentityProvider { get; set; } = new IdentityProviderSettings();
    }
}
