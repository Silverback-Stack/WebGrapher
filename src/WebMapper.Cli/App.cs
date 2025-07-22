using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Events.Core.Types;

namespace WebMapper.Cli
{
    internal class App
    {
        private readonly IEventBus _eventBus;

        public App() {

            //EVENT SERVICE
            _eventBus = EventBusService.Configure();


            //CRAWLER SERVICE
            Task.Run(async () => 
                await CrawlerService.ConfigureAsync(_eventBus));


            //SCRAPER SERVICE
            Task.Run(async () => 
                await ScraperService.ConfigureAsync(_eventBus));


            //PARSER SERVICE
            Task.Run(async () =>
                await ParserService.ConfigureAsync(_eventBus));


            //NORMALISATION SERVICE
            Task.Run(async () =>
                await NormalisationService.ConfigureAsync(_eventBus));

            //GRAPHING SERVICE
            Task.Run(async () =>
                await GraphingService.ConfigureAsync(_eventBus));

            //STREAMING SERVICE
            //Task.Run(() => StreamingFactory.SetupAsync(_eventBus));
        }



        public async Task Start()
        {
            while (true)
            {
                //READ INPUT:
                var url = GetUrl();

                //SUBMIT INPUT:
                if (url != null)
                    await SubmitUrl(url);
            }
        }

        /// <summary>
        /// Reads the Url input from the console.
        /// </summary>
        /// <returns></returns>
        private Uri? GetUrl()
        {
            Console.WriteLine();
            Console.WriteLine("Enter Url:");
            var url = Console.ReadLine();

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Console.WriteLine("Invalid Url. Use format: https://www.example.com");
                return null;
            }

            return uri;
        }

        /// <summary>
        /// Submits a Url for processing.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task SubmitUrl(Uri url)
        {
            var crawlPageEvent = new CrawlPageEvent(
                url, 
                containerId: Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                correlationId: Guid.NewGuid());

            crawlPageEvent.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
            crawlPageEvent.ClientAccepts = "text/html";

            //crawlPageEvent.PathFilters = new string[] { "/movie/", "/tv/", "/person/" }; //https://www.themoviedb.org/movie/
            //crawlPageEvent.PathFilters = new string[] { "/artist/", "/album/", "/track/" }; //https://www.theaudiodb.com/chart_artists
            crawlPageEvent.PathFilters = new string[] { "/title/", "/name/" }; //https://www.imdb.com/
            crawlPageEvent.FollowExternalLinks = false;
            crawlPageEvent.RemoveQueryStrings = true;
            crawlPageEvent.MaxDepth = 5;

            await _eventBus.PublishAsync(crawlPageEvent);
            Console.WriteLine($"{url.AbsoluteUri} was submitted.");
        }


    }
}
