//using System.IO;
//using System.Reflection;

//namespace Logging.Core
//{
//    public class FileAppLoggerAdapter : BaseAppLogger
//    {
//        private const string FILE_CONTAINER = "Logs";
//        private const string FILE_DEBUG = "Debug.log";
//        private const string FILE_INFO = "Info.log";
//        private const string FILE_WARN = "Warn.log";
//        private const string FILE_ERROR = "Error.log";
//        private const string FILE_CRITICAL = "Critical.log";

//        private static readonly object _fileLock = new();

//        public FileAppLoggerAdapter(string name) : base(name)
//        {
//            CreateContainers(name);
//        }

//        private string FilePath { get; set; } = string.Empty;

//        public override void Dispose()
//        {
//            //fully managed - nothing to close
//        }

//        protected override void Log(AppLoggerLevel level, string message, object? context = null)
//        {
//            //TODO: serialise object to string
//            //append to message

//            switch (level)
//            {
//                case AppLoggerLevel.Debug:
//                    AppendToFile(FILE_DEBUG, message);
//                    break;
//                case AppLoggerLevel.Info:
//                    AppendToFile(FILE_INFO, message);
//                    break;
//                case AppLoggerLevel.Warn:
//                    AppendToFile(FILE_WARN, message);
//                    break;
//                case AppLoggerLevel.Error:
//                    AppendToFile(FILE_ERROR, message);
//                    break;
//                case AppLoggerLevel.Critical:
//                    AppendToFile(FILE_CRITICAL, message);
//                    break;
//                default:
//                    throw new NotSupportedException($"Logging level '{level}' is not supported.");
//            }
//        }


//        private void AppendToFile(string filename, string message)
//        {
//            lock (_fileLock)
//            {
//                File.AppendAllText(
//                    $"{FilePath}{filename}",
//                    $"{DateTimeOffset.UtcNow.ToString()} {message}\n");
//            }
//        }

//        private void CreateContainers(string name)
//        {
//            if (!Directory.Exists(FILE_CONTAINER))
//            {
//                Directory.CreateDirectory(FILE_CONTAINER);
//            }
//            FilePath = $"{FILE_CONTAINER}{Path.DirectorySeparatorChar}";

//            if (!string.IsNullOrWhiteSpace(name))
//            {
//                if (!Directory.Exists(FilePath + name))
//                {
//                    Directory.CreateDirectory(FilePath + name);
//                }
//                FilePath += $"{name}{Path.DirectorySeparatorChar}";
//            }
//        }

//    }
//}
