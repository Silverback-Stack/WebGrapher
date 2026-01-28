using Microsoft.Extensions.Configuration;

namespace App.Settings
{
    public static class ConfigurationLoader
    {
        public static string GetEnvironmentName()
        {
            return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
                ?? "Production";
        }

        public static IConfiguration LoadConfiguration(string path)
        {
            var environment = GetEnvironmentName();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)

                // Service settings
                .AddJsonFile($"{path}/appsettings.json", optional: false, reloadOnChange: true)

                // Logging settings
                .AddJsonFile($"Logging/appsettings.json", optional: false, reloadOnChange: true);

            if (!string.IsNullOrEmpty(environment))
            {
                // Service Environment overrides (optional = true)
                builder.AddJsonFile($"{path}/appsettings.{environment}.json", optional: true, reloadOnChange: true);

                // Logging Environment overrides (optional = true)
                builder.AddJsonFile($"Logging/appsettings.{environment}.json", optional: true, reloadOnChange: true);
            }

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
