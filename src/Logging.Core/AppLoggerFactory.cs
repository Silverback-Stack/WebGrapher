using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Logging.Core
{
    public class AppLoggerFactory
    {
        public static IAppLogger CreateLogger(
            string serviceName, 
            AppLoggerOptions loggerType,
            object? logger = null)
        {
            switch (loggerType)
            {
                //case AppLoggerOptions.Console:
                //    return new ConsoleAppLoggerAdapter(serviceName);

                //case AppLoggerOptions.File:
                //    return new FileAppLoggerAdapter(serviceName);

                case AppLoggerOptions.Serilog:
                    if (logger is Serilog.ILogger serilogLogger)
                        return new SerilogAppLoggerAdapter(serviceName, serilogLogger);

                    throw new ArgumentNullException(
                        "Logger must be specified when using the Serilog Logging Provider.");

                case AppLoggerOptions.Microsoft:
                    if (logger is ILogger microsoftLogger)
                        return new MicrosoftAppLoggerAdapter(serviceName, microsoftLogger);

                    throw new ArgumentNullException(
                        "Logger must be specified when using the Microsoft Logging Provider.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(loggerType),
                        $"Unsupported logger type: {loggerType}");
            }
        }
    }
}
