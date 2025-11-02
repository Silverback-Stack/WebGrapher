using Graphing.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Graphing.WebApi;
using Auth.WebApi;

namespace Graphing.WorkerService
{
    internal class WebApiHostedService : IHostedService
    {
        private readonly IPageGrapher _pageGrapher;
        private readonly GraphingWebApiSettings _graphingWebApiSettings;
        private readonly AuthSettings _authSettings;
        private WebApplication? _app;

        public WebApiHostedService(
            IPageGrapher pageGrapher, 
            GraphingWebApiSettings graphingWebApiSettings,
            AuthSettings authSettings)
        {
            _pageGrapher = pageGrapher;
            _graphingWebApiSettings = graphingWebApiSettings;
            _authSettings = authSettings;
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
            builder.Services.AddGraphingWebApi(_pageGrapher, _graphingWebApiSettings, _authSettings);

            _app = builder.Build();

            _app.UseGraphingWebApi(_graphingWebApiSettings);

            _app.Urls.Add(_graphingWebApiSettings.Host);

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
