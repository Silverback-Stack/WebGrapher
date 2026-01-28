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
        private readonly GraphingWebApiConfig _graphingWebApiConfig;
        private readonly AuthConfig _authConfig;
        private WebApplication? _app;

        public WebApiHostedService(
            IPageGrapher pageGrapher, 
            GraphingWebApiConfig graphingWebApiConfig,
            AuthConfig authConfig)
        {
            _pageGrapher = pageGrapher;
            _graphingWebApiConfig = graphingWebApiConfig;
            _authConfig = authConfig;
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
            builder.Services.AddGraphingWebApi(_pageGrapher, _graphingWebApiConfig, _authConfig);

            _app = builder.Build();

            _app.UseGraphingWebApi(_graphingWebApiConfig);

            _app.Urls.Add(_graphingWebApiConfig.Host);

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
