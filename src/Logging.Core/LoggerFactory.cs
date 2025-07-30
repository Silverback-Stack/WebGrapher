using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logging.Core.Adapters;

namespace Logging.Core
{
    public class LoggerFactory
    {
        public static ILogger CreateLogger(
            string serviceName, 
            LoggerOptions loggerType,
            object? logger = null)
        {
            switch (loggerType)
            {
                case LoggerOptions.Serilog:
                    if (logger is Serilog.ILogger serilogLogger)
                        return new SerilogLoggerAdapter(serviceName, serilogLogger);

                    throw new ArgumentNullException(
                        "Logger must be specified when using the Serilog Logging Provider.");

                case LoggerOptions.Microsoft:
                    if (logger is Microsoft.Extensions.Logging.ILogger microsoftLogger)
                        return new MicrosoftLoggerAdapter(serviceName, microsoftLogger);

                    throw new ArgumentNullException(
                        "Logger must be specified when using the Microsoft Logging Provider.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(loggerType),
                        $"Unsupported logger type: {loggerType}");
            }
        }
    }
}
