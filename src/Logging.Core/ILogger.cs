using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logging.Core
{
    public interface ILogger : IDisposable
    {
        void LogDebug(
            string message, 
            string? correlationId = null, 
            object? context = null);

        void LogInformation(
            string message, 
            string? correlationId = null, 
            object? context = null);

        void LogWarning(
            string message, string? 
            correlationId = null, 
            object? context = null);

        void LogError(
            string message, 
            string? correlationId = null, 
            object? context = null);

        void LogCritical(
            string message, 
            string? correlationId = null, 
            object? context = null);
    }
}
