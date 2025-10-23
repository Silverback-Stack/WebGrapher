using System;

namespace Streaming.WebApi
{
    public static class EnvironmentHelpers
    {
        /// <summary>
        /// Returns true if the current process is running locally 
        /// (i.e., not hosted in Azure App Service or a container).
        /// </summary>
        public static bool IsRunningLocally()
        {
            var websiteInstance = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
            var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");

            // If both are empty, we're likely running locally
            return string.IsNullOrEmpty(websiteInstance) && string.IsNullOrEmpty(inContainer);
        }
    }
}
