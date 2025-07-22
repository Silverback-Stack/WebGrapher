
using Events.Core.Bus;

namespace ParserService
{
    public interface IPageParser
    {
        void Start();
        Page Parse(string content);
    }
}
