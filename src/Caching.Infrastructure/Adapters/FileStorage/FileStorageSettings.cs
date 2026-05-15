using System;

namespace Caching.Infrastructure.Adapters.FileStorage
{
    public class FileStorageSettings
    {
        public int AbsoluteExpiryHours { get; set; } = 24;
    }
}
