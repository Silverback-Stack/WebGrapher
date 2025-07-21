using System.IO;
using System.Reflection;

namespace Logging.Core
{
    public class FileLogger : BaseLogger
    {
        private const string FILE_CONTAINER = "Logs";
        private const string FILE_DEBUG = "Debug.log";
        private const string FILE_INFO = "Info.log";
        private const string FILE_WARN = "Warn.log";
        private const string FILE_ERROR = "Error.log";
        private const string FILE_CRITICAL = "Critical.log";

        private static readonly object _fileLock = new();

        public FileLogger(string name) : base(name)
        {
            CreateContainers(name);
        }

        private string FilePath { get; set; } = string.Empty;

        internal override void Log(LoggingLevel level, string message)
        {
            switch (level)
            {
                case LoggingLevel.Debug:
                    AppendToFile(FILE_DEBUG, message);
                    break;
                case LoggingLevel.Info:
                    AppendToFile(FILE_INFO, message);
                    break;
                case LoggingLevel.Warn:
                    AppendToFile(FILE_WARN, message);
                    break;
                case LoggingLevel.Error:
                    AppendToFile(FILE_ERROR, message);
                    break;
                case LoggingLevel.Critical:
                    AppendToFile(FILE_CRITICAL, message);
                    break;
                default:
                    throw new NotSupportedException($"Logging level '{level}' is not supported.");
            }
        }

        private void AppendToFile(string filename, string message)
        {
            lock (_fileLock)
            {
                File.AppendAllText(
                    $"{FilePath}{filename}",
                    $"{DateTimeOffset.UtcNow.ToString()} {message}\n");
            }
        }

        private void CreateContainers(string name)
        {
            if (!Directory.Exists(FILE_CONTAINER))
            {
                Directory.CreateDirectory(FILE_CONTAINER);
            }
            FilePath = $"{FILE_CONTAINER}{Path.DirectorySeparatorChar}";

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (!Directory.Exists(FilePath + name))
                {
                    Directory.CreateDirectory(FilePath + name);
                }
                FilePath += $"{name}{Path.DirectorySeparatorChar}";
            }
        }

    }
}
