using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logging.Core
{
    public class LoggingFactory
    {
        public static ILogger Create(LoggingOptions loggerType, string name)
        {
            switch (loggerType)
            {
                case LoggingOptions.Console:
                    return new ConsoleLogger(name);

                case LoggingOptions.File:
                    return new FileLogger(name);    

                default:
                    throw new ArgumentOutOfRangeException(nameof(loggerType),
                        $"Unsupported logger type: {loggerType}");
            }
        }
    }
}
