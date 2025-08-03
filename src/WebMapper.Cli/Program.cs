// See https://aka.ms/new-console-template for more information
using WebMapper.Cli;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using WebMapper.Cli.Service.Crawler;
using WebMapper.Cli.Service.Streaming;

internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var webMapper = new WebMapperApp();
            await webMapper.InitializeAsync();
        }
        catch (Exception ex) //global exception handler
        {
            Console.WriteLine($"Critical Exception: {ex.Message} Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine("The application was terminated.");
            Console.ReadKey();
        }
    }
}