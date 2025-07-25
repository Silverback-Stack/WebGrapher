
using Events.Core.Bus;

namespace ParserService
{
    public interface IPageParser
    {
        Page Parse(string content);
    }
}
