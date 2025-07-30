
using Graphing.Core.Models;

namespace Graphing.Core
{
    public interface IGraphAnalyser
    {
        int AverageLinksPerNode();
        void FindPagesByDomain(string domain);
        void FindReachablePages(string fromUrl, int maxDepth);
        void GetDeadEnds();
        void GetIsolatedNodes();
        void GetMostLinkedPages(int topN);
        void GetShortestPath(string fromUrl, string toUrl);
        IEnumerable<Node> SearchPagesByKeyword(string keyword);
        int TotalEdges();
        int TotalNodes();
    }
}