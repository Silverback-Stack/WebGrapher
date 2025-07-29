using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.Helpers
{
    public static class EventScheduleHelper
    {
        private const int DEFAULT_MAX_DELAY_IN_SECONDS = 5;

        /// <summary>
        /// Adds a random delay up to maxSeconds to the specified DateTimeOffset.
        /// </summary>
        public static DateTimeOffset? AddRandomDelayTo(DateTimeOffset? value, int maxSeconds = DEFAULT_MAX_DELAY_IN_SECONDS)
        {
            var delayInMilliseconds = Random.Shared.Next(1000, (maxSeconds + 1) * 1000);
            return value.HasValue ? value.Value.AddMilliseconds(delayInMilliseconds) : null;
        }
    }
}
