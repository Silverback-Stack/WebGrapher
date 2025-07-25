using Events.Core.Bus;

namespace Graphing.Core
{
    public interface IGraph : IDisposable
    {
        IGraphAnalyser GraphAnalyser { get; }

        bool IsNodePopulated(string id);

        Node AddNode(
            string id, 
            string title, 
            string keywords,
            DateTimeOffset? sourceLastModified,
            IEnumerable<string> edges);

        void RemoveNode(string id);

    }
}
