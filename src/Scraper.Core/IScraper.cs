using System;

namespace ScraperService
{
    public interface IScraper
    {
        Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? clientAccept);
    }
}
