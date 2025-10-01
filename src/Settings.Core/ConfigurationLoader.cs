using Microsoft.Extensions.Configuration;

namespace Settings.Core
{
    public static class ConfigurationLoader
    {
        public static IConfigurationRoot LoadConfiguration(string serviceName)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)

                // Base settings
                .AddJsonFile($"{serviceName}/appsettings.json", optional: true, reloadOnChange: true)

                // Environment overrides
                .AddJsonFile($"{serviceName}/appsettings.{environment}.json", optional: true, reloadOnChange: true)

                .AddEnvironmentVariables()
                .Build();

            return configuration;
        }

        public static T BindSection<T>(this IConfiguration config, string sectionName) where T : new()
        {
            var settings = new T();
            config.GetSection(sectionName).Bind(settings);
            return settings;
        }
    }
}
