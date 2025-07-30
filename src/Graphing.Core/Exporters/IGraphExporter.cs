using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphing.Core.Models;

namespace Graphing.Core.Exporters
{
    public interface IGraphExporter
    {
        string Export(IReadOnlyDictionary<string, Node> graph);
    }

}
