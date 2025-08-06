// See https://aka.ms/new-console-template for more information
using WebMapper.Cli;
using System.Text;
using Graphing.Core.Version2;

internal class Program
{
    private static async Task Main(string[] args)
    {
        //Enable full code page support(especially outside UTF-8 / UTF-16):
        //Required for scraping reponses from servers.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); 

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