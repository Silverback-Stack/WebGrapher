using Auth.WebApi.Auth.IdentityProviders.AppSettings;
using Auth.WebApi.Auth.IdentityProviders.Auth0;
using Auth.WebApi.Auth.IdentityProviders.AzureAD;

namespace Auth.WebApi.Auth.IdentityProviders
{
    public class IdentityProviderSettings
    {
        public IdentityProviderType ProviderType { get; set; } = IdentityProviderType.Local;

        public LocalSettings Local { get; set; } = new LocalSettings();

        public AzureADSettings AzureAD { get; set; } = new AzureADSettings();

        public Auth0Settings Auth0 { get; set; } = new Auth0Settings();
    }

}
