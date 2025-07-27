
using Events.Core.Bus;

namespace ParserService
{
    public interface IPageParser
    {
        PageItem Parse(string content);
    }
}
