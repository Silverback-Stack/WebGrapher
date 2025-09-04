
namespace Normalisation.Core.Processors
{
    public class ProcessorSettings
    {
        public string TitleXPath { get; set; } = ".//title";
        public string LinksXPath { get; set; } = ".//a[@href]";
        public string DefaultLanguageIso2Code { get; set; } = "en";
        public string DefaultLanguageIso3Code { get; set; } = "eng";
    }
}
