using Microsoft.Extensions.Configuration;

namespace Settings.Core
{
    public static class ConfigurationLoader
    {
        public static IConfiguration LoadConfiguration(string serviceName)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
               ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
               ?? Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)

                // Service settings
                .AddJsonFile($"{serviceName}/appsettings.json", optional: true, reloadOnChange: true)

                // Logging settings
                .AddJsonFile($"Shared.Logging/appsettings.json", optional: true, reloadOnChange: true);

            if (!string.IsNullOrEmpty(environment))
            {
                // Service Environment overrides
                builder.AddJsonFile($"{serviceName}/appsettings.{environment}.json", optional: true, reloadOnChange: true);

                // Logging Environment overrides
                builder.AddJsonFile($"Shared.Logging/appsettings.{environment}.json", optional: true, reloadOnChange: true);
            };

            var configuration = builder
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
