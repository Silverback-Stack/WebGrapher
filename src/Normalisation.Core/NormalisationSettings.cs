
using Normalisation.Core.Processors;

namespace Normalisation.Core
{
    public class NormalisationSettings
    {
        public string ServiceName { get; set; } = "Normalisation";
        public int MaxTitleLength { get; set; } = 100;
        public int MaxSummaryWords { get; set; } = 100;
        public int MaxKeywords { get; set; } = 300; //a page of text
        public int MaxKeywordTags { get; set; } = 10;
        public int MaxLinksPerPage { get; set; } = 1000;

        public string[] AllowableLinkSchemas = ["http", "https"];
        public ProcessorSettings Processors { get; set; } = new ProcessorSettings();
}
}
