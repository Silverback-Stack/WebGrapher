
namespace Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin
{
    public class AzureCosmosGremlinSettings
    {
        public string Hostname { get; set; } = string.Empty;
        public int Port { get; set; } = 443;
        public bool EnableSsl { get; set; } = true;
        public string PrimaryKey { get; set; } = string.Empty;
        public string Database {  get; set; } = string.Empty;
        public string Graph {  get; set; } = string.Empty;
        public int MaxQueryRetries { get; set; } = 5;
    }
}
