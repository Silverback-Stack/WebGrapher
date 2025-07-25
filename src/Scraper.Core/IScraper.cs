using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Requests.Core;

namespace ScraperService
{
    public interface IScraper
    {
        Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? clientAccept);
    }
}
