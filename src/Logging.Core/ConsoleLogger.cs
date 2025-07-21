using System.Xml.Linq;

namespace Logging.Core
{
    public class ConsoleLogger : BaseLogger
    {
        private string _name;

        public ConsoleLogger(string name) : base(name) {
            _name = name;
        }
        
        internal override void Log(LoggingLevel level, string message)
        {
            Console.WriteLine($"{_name}:{level.ToString()}: {message}");
        }
    }
}
