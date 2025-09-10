using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.RateLimiters
{
    public class RateLimiterConfig
    {
        public Dictionary<Type, int> Limits { get; } = new();

        public RateLimiterConfig Limit<TEvent>(int maxConcurrency) where TEvent : class
        {
            Limits[typeof(TEvent)] = maxConcurrency;
            return this;
        }
    }
}
