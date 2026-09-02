
using Normalisation.Core.Processors;

namespace Normalisation.Core
{
    public class NormalisationSettings
    {
        public string ServiceName { get; set; } = "Normalisation";


        // Data bounds - limits normalised data to less than 100 KB for efficient event messaging
        public int MaxTitleLength { get; set; } = 100;
        public int MaxSummaryLength { get; set; } = 750; // approximately a paragraph of text
        public int MaxKeywordsLength { get; set; } = 3000; // approximately a page of text
        public int MaxTags { get; set; } = 10; // most frequent keywords
        public int MaxLinks { get; set; } = 100;
        public int MaxLinksBytes { get; set; } = 64 * 1024; // 64 KB

        // Link options
        public string[] AllowedLinkSchemes { get; set; } = ["http", "https"];

        // Fallback options
        public string LanguageDetectionFallbackIso2Code { get; set; } = "en";
        public string LanguageDetectionFallbackIso3Code { get; set; } = "eng";
    }
}
