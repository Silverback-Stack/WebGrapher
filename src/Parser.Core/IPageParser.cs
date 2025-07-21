
using Events.Core.Bus;

namespace ParserService
{
    public interface IPageParser : IEventBusLifecycle
    {
        PageDto Parse(string content);
    }
}
