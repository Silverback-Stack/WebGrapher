using Events.Core.Dtos;
using Events.Core.Events;
using Requests.Core;
using System;

namespace Scraper.Core
{
    public interface IPageScraper
    {
        Task StartAsync();
        
        Task StopAsync();

        Task ScrapePageAsync(ScrapePageEvent evt);

    }
}
