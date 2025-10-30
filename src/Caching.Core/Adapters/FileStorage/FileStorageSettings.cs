using System;

namespace Caching.Core.Adapters.FileStorage
{
    public class FileStorageSettings
    {
        public string ContainerName { get; set; } = "file.cache";
        public int AbsoluteExpiryHours { get; set; } = 24;
    }
}
