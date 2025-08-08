using System;

namespace Graphing.Core.WebGraph.Adapters.SigmaJs
{
    public class SigmaNode
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double Size { get; set; } = 1.0;
        public string Color { get; set; } = "#888";
    }
}
