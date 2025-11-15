
namespace Graphing.WebApi
{
    public class GraphingWebApiSettings
    {
        public string Host { get; set; } = string.Empty;

        public IEnumerable<string> AllowedOrigins { get; set; } = new List<string>();

        public SwaggerSettings Swagger { get; set; } = new SwaggerSettings();
    }

    public class SwaggerSettings
    {
        public string EndpointUrl { get; set; } = string.Empty;
        public string EndpointName { get; set; } = string.Empty;
        public string RoutePrefix { get; set; } = string.Empty;
    }

}
