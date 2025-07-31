
namespace Parser.Core
{
    public interface IPageParser
    {
        PageItem Parse(string content);
    }
}
