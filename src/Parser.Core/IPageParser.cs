
using Events.Core.Bus;

namespace ParserService
{
    public interface IPageParser : IEventBusLifecycle
    {
        Page Parse(string content);
    }
}
