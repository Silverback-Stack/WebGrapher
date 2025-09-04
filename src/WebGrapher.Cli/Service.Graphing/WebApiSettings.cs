using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebGrapher.Cli.Service.Graphing
{
    public class WebApiSettings
    {
        public string Host { get; set; } = "http://localhost:5000";
        public string SwaggerEndpointUrl { get; set; } = "/swagger/v1/swagger.json";
        public string SwaggerEndpointName { get; set; } = "Grapher API V1";
        public string SwaggerRoutePrefix { get; set; } = "swagger";
    }
}
