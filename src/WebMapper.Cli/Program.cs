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
        try
        {
            var app = new App();
            await app.Run();

        }
        catch (Exception ex) //global exception handler
        {
            Console.WriteLine($"Critical Exception: {ex.Message} Inner Exception: {ex.InnerException?.Message}");
        }

        Console.WriteLine("The application was terminated.");
        Console.ReadKey();
    }
}