using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Events.Core.Bus;

namespace Crawler.Core
{
    public partial class LiteDbCrawlerAdapter : BaseCrawler
    {
        private const string FILE_CONTAINER = "Data";
        private const string FILE_DATABASE = "LiteDbData";

        private readonly LiteDatabase _database;

        public LiteDbCrawlerAdapter(IEventBus eventBus) : base(eventBus)
        {
            if (!Directory.Exists(FILE_CONTAINER))
            {
                Directory.CreateDirectory(FILE_CONTAINER);
            }
            _database = new LiteDatabase($"{FILE_CONTAINER}{Path.DirectorySeparatorChar}{FILE_DATABASE}");
        }

        internal override DateTimeOffset? GetUrl(string key)
        {
            var col = _database.GetCollection<UrlItem>(nameof(UrlItem));
            var item = col.FindOne(x => x.Key == key && x.ExpireAt > DateTimeOffset.UtcNow);
            if (item != null)
            {
                return item?.Value;
            }
            return null;
        }

        internal override void SetUrl(string key, DateTimeOffset value)
        {
            var col = _database.GetCollection<UrlItem>(nameof(UrlItem));
            col.Upsert(new UrlItem
            {
                Key = key,
                Value = value,
                ExpireAt = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(DEFAULT_ABSOLUTE_EXPIRY_MINUTES))
            });
        }

        private class UrlItem
        {
            public int Id { get; set; }
            public required string Key { get; set; }
            public DateTimeOffset Value { get; set; }
            public DateTimeOffset ExpireAt { get; set; }
        }

        private class SessionItem
        {
            public int Id { get; set; }
            public required string Key { get; set; }
            public int Value { get; set; }
            public DateTimeOffset ExpireAt { get; set; }
        }
    }
}
