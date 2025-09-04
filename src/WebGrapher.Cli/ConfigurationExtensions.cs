using System;
using Microsoft.Extensions.Configuration;

namespace WebGrapher.Cli
{
    public static class ConfigurationExtensions
    {
        public static T BindSection<T>(this IConfiguration configuration, string sectionName) where T : new()
        {
            var settings = new T();
            configuration.GetSection(sectionName).Bind(settings);
            return settings;
        }
    }

}
