using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core
{
    public class ScraperSettings
    {
        public string ServiceName { get; set; } = "Scraper";
        public int ContentMaxBytes { get; set; } = 4_194_304; //4 Mb
    }
}
