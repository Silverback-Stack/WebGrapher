using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.Bus
{
    public interface IEventBusLifecycle
    {
        /// <summary>
        /// Registers event handlers or subscriptions for this service.
        /// </summary>
        void Subscribe();

        /// <summary>
        /// Unsubscribes or cleans up event handlers.
        /// </summary>
        void Unsubscribe();

    }
}
