using System;

namespace Auth.WebApi.IdentityProviders.AzureAD
{
    public class AzureADSettings
    {
        public string Instance { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string LogoutUrl { get; set; } = string.Empty;
    }
}
