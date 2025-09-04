
namespace Events.Core.Helpers
{
    public static class EventScheduleHelper
    {
        /// <summary>
        /// Adds a random delay between minSeconds and maxSeconds to the specified DateTimeOffset.
        /// </summary>
        public static DateTimeOffset? AddRandomDelayTo(DateTimeOffset? value, int minSeconds, int maxSeconds)
        {
            var delayInMilliseconds = Random.Shared.Next(
                minSeconds * 1000, (maxSeconds + 1) * 1000);
            return value.HasValue ? value.Value.AddMilliseconds(delayInMilliseconds) : null;
        }
    }
}
