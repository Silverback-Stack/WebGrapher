using System;

namespace Scraper.Core
{
    public interface IScraper
    {
        Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? clientAccept);
    }
}
