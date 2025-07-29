using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Events.Core.EventTypes;

namespace WebMapper.Cli
{
    internal class App
    {
        private readonly IEventBus _eventBus;

        public App() {

            //EVENT SERVICE
            _eventBus = EventBusService.Start();


            //CRAWLER SERVICE
            Task.Run(async () => 
                await CrawlerService.InitializeAsync(_eventBus));


            //SCRAPER SERVICE
            Task.Run(async () => 
                await ScraperService.InitializeAsync(_eventBus));


            //PARSER SERVICE
            Task.Run(async () =>
                await ParserService.InitializeAsync(_eventBus));


            //NORMALISATION SERVICE
            Task.Run(async () =>
                await NormalisationService.InitializeAsync(_eventBus));

            //GRAPHING SERVICE
            Task.Run(async () =>
                await GraphingService.InitializeAsync(_eventBus));

            //STREAMING SERVICE
            //Task.Run(() => StreamingFactory.StartAsync(_eventBus));
        }



        public async Task Run()
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
                Console.WriteLine("Invalid Url. Format: https://www.example.com");
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
            //Accept examples:

            //HTML AND IMAGES
            //Accept: text/html,image/*;q=0.9

            var crawlPageEvent = new CrawlPageEvent(
                url,
                containerId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                correlationId: Guid.NewGuid(),
                followExternalLinks: false,
                removeQueryStrings: true,
                maxDepth: 5,

                //https://www.themoviedb.org/movie/
                //pathFilters: new string[] { "/movie/", "/tv/", "/person/" },

                //https://www.theaudiodb.com/chart_artists
                //pathFilters: new string[] { "/artist/", "/album/", "/track/" },
                //pathFilters: new string[] { "/artist/", "/album/" },

                //https://www.imdb.com/
                //pathFilters: new string[] { "/title/", "/name/" },

                //https://en.wikipedia.org/wiki/Terminator_2%3A_Judgment_Day
                pathFilters: new string[] { },

                userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
                //userAccepts: "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8"
                //userAccepts: "text/html,image/*;q=0.9"
                userAccepts: "text/html"
                );

            await _eventBus.PublishAsync(crawlPageEvent);
        }


    }
}
