using Graphing.Core.Models;

namespace Graphing.Core
{
    public interface IGraph : IDisposable
    {
        IGraphAnalyser GraphAnalyser { get; }

        Task<Node?> GetNodeAsync(string id);

        Task<Node?> SetNodeAsync(Node node);

        Task DeleteNodeAsync(string id);
    }
}
