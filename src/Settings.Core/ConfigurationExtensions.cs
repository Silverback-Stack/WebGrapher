
using Microsoft.Extensions.Configuration;

namespace Settings.Core
{
    public static class ConfigurationExtensions
    {
        public static string GetEnvironmentName(this IConfiguration configuration)
        {
            return ConfigurationLoader.GetEnvironmentName();
        }
    }
}
