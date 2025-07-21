using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;

namespace ScraperService
{
    public interface IScraper : IEventBusLifecycle
    {
        Task<ResponseDto> GetAsync(Uri url, string userAgent, string clientAccept);
    }
}
