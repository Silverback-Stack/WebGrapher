// See https://aka.ms/new-console-template for more information
using WebGrapher.Cli;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // .NET only supports UTF encodings by default.
        // Enable support for legacy encodings used by some web pages and older systems:
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); 

        try
        {
            var webGrapher = new WebGrapherApp();
            await webGrapher.InitializeAsync();
        }
        catch (Exception ex) //global exception handler
        {
            Console.WriteLine($"Critical Exception: {ex.Message} Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine("The application was terminated.");
            Console.ReadKey();
        }
    }

}