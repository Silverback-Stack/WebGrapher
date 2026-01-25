
using Microsoft.Extensions.Configuration;

namespace App.Settings
{
    public static class ConfigurationExtensions
    {
        public static string GetEnvironmentName(this IConfiguration configuration)
        {
            return ConfigurationLoader.GetEnvironmentName();
        }
    }
}
