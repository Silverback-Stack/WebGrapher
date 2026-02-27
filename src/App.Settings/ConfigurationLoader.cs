using Microsoft.Extensions.Configuration;

namespace App.Settings
{
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Builds an <see cref="IConfiguration"/> by loading appsettings.json from one or more
        /// specified paths, applying optional environment-specific overrides, and merging
        /// environment variables on top. Returns a single combined configuration instance.
        /// </summary>
        public static IConfiguration LoadConfiguration(string environmentName, params string[] paths)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
                throw new ArgumentException("Environment name must be provided.", nameof(environmentName));

            if (paths.Length == 0)
                throw new ArgumentException(
                    "At least one configuration path must be provided.", nameof(paths));

            if (paths.Any(p => string.IsNullOrWhiteSpace(p)))
                throw new ArgumentException(
                    "Configuration paths must not be empty.", nameof(paths));

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory);

            foreach (var path in paths)
                AddJsonFile(builder, path, environmentName);

            return builder
                .AddEnvironmentVariables()
                .Build();
        }

        private static void AddJsonFile(
            IConfigurationBuilder builder,
            string path,
            string environmentName)
        {
            // Load base settings
            var baseSettings = Path.Combine(path, "appsettings.json");
            builder.AddJsonFile(baseSettings, optional: false, reloadOnChange: true);

            // Load environment overrides
            var environmentSettings = Path.Combine(path, $"appsettings.{environmentName}.json");
            builder.AddJsonFile(environmentSettings, optional: true, reloadOnChange: true);
        }

        /// <summary>
        /// Binds a configuration section to a new strongly-typed instance of <typeparamref name="T"/>.
        /// </summary>
        public static T BindSection<T>(this IConfiguration config, string sectionName) where T : new()
        {
            var settings = new T();
            config.GetSection(sectionName).Bind(settings);
            return settings;
        }

    }
}
