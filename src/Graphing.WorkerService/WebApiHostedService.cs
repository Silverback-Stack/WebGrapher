using Graphing.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Graphing.WebApi;

namespace Graphing.WorkerService
{
    internal class WebApiHostedService : IHostedService
    {
        private readonly IPageGrapher _pageGrapher;
        private readonly WebApiSettings _webApiSettings;
        private WebApplication? _app;

        public WebApiHostedService(
            IPageGrapher pageGrapher, 
            WebApiSettings webApiSettings)
        {
            _pageGrapher = pageGrapher;
            _webApiSettings = webApiSettings;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
                ContentRootPath = AppContext.BaseDirectory
            });

            // Use existing logger
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);

            // Add the Graphing API
            builder.Services.AddGraphingWebApi(_pageGrapher);

            _app = builder.Build();

            _app.UseGraphingWebApi(_webApiSettings);

            _app.Urls.Add(_webApiSettings.Host);

            await _app.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.WaitForShutdownAsync();
                await _app.DisposeAsync();
            }
            Log.CloseAndFlush();
        }
    }
}
