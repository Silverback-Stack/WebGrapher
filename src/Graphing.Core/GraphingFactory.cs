using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;

namespace Graphing.Core
{
    public class GraphingFactory
    {
        public static IGraph Create(IEventBus eventBus)
        {
            return new MemoryGraph(eventBus);
        }
    }
}
