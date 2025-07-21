// See https://aka.ms/new-console-template for more information
using WebMapper.Cli;
using Logging.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var _logger = LoggingFactory.Create(LoggingOptions.Console, nameof(WebMapper.Cli));

        try
        {
            var app = new App();
            await app.Start();

        }
        catch (Exception ex) //global exception handler
        {
            _logger.LogCritical($"{ex.Message}");
        }

        Console.WriteLine("The application was terminated.");
        Console.ReadKey();
    }
}