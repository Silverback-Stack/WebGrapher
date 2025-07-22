using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;

namespace Graphing.Core
{
    public class GraphingFactory
    {
        public static IGraph Create(IAppLogger appLogger, IEventBus eventBus)
        {
            return new MemoryGraph(appLogger, eventBus);
        }
    }
}
