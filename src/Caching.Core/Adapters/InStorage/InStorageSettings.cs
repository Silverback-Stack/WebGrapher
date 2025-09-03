using System;

namespace Caching.Core.Adapters.InStorage
{
    public class InStorageSettings
    {
        public string ContainerName { get; set; } = "storage.cache";
        public int AbsoluteExpirationHours { get; set; } = 0;
    }
}
